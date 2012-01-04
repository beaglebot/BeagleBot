/*
 *  powersupply.c
 *
 *  Exposes the charging state of two BQ24123 LiPo charger ICs over an I2C interface.
 *
 *  Copyright (C) Ben Galvin 2012
 *
 *  Feel free to do whatever you want with the code. Use at your own risk.
 *
 *  Microcontroller Pin Setup
 *  =========================
 *  NOTE: The PG and STAT pins on the BQ24123 are all open collector, so 0V => High, 5V => Low.
 *  PIN5/PA0:           CEA (output)
 *  PIN6/PD2:           PGA 
 *  PIN7/PD3/INT1:      STAT1A
 *  PIN8/PD4:           STAT2A
 *  PIN13/PB1:          CEB (output)
 *  PIN14/PB2:          PGB
 *  PIN15/PB3/PCINT3:   STAT1B
 *  PIN16/PB4:          STAT2B
 *
 */

#include <avr/io.h>
#include <avr/interrupt.h>
#include <util/delay.h>
#include <stdbool.h>
#include "../common/usi_slave.h"


#define I2C_SLAVE_ADDR  0x30
#define NUM_SECONDS_TO_DISABLE_AFTER_CHARGE (20*60)


/* Globals */
volatile uint32_t time_in_seconds = 0;

bool charger_a_is_charging;
uint32_t charger_a_finished_timestamp = 0xFFFFFFFF;
volatile uint32_t charger_a_disabled_counter = 0;
bool charger_a_disabled_by_i2c = false;

bool charger_b_is_charging;
uint32_t charger_b_finished_timestamp = 0xFFFFFFFF;
volatile uint16_t charger_b_disabled_counter = 0;
bool charger_b_disabled_by_i2c = false;


/* Returns the state of charger A. */
uint8_t get_status_a()
{
    uint8_t status = 0;

    if ((PIND & (1 << PD2)) == 0) status |= (1 << 0);
    if ((PIND & (1 << PD3)) == 0) status |= (1 << 1);
    if ((PIND & (1 << PD4)) == 0) status |= (1 << 2);
    if ((PINA & (1 << PA0)) == 0) status |= (1 << 3);
    if (charger_a_disabled_counter != 0) status |= (1 << 4);
    if (charger_a_disabled_by_i2c != 0) status |= (1 << 5);
	if (charger_a_finished_timestamp != 0xFFFFFFFF) status |= (1 << 6);

    return status;
}

/* Returns the state of charger B. */
uint8_t get_status_b()
{
    uint8_t status = 0;

    if ((PINB & (1 << PB2)) == 0) status |= (1 << 0);
    if ((PINB & (1 << PB3)) == 0) status |= (1 << 1);
    if ((PINB & (1 << PB4)) == 0) status |= (1 << 2);
    if ((PINB & (1 << PB1)) == 0) status |= (1 << 3);
    if (charger_b_disabled_counter != 0) status |= (1 << 4);
    if (charger_b_disabled_by_i2c != 0) status |= (1 << 5);
	if (charger_b_finished_timestamp != 0xFFFFFFFF) status |= (1 << 6);

    return status;
}

/* Disables or enables charger A through I2C. */
void set_charger_a_disabled(uint8_t is_disabled)
{
    if (is_disabled) {

        charger_a_disabled_by_i2c = true;
        charger_a_disabled_counter = 0;
		charger_a_finished_timestamp = 0xFFFFFFFF;

        /* Set CEA high. */
        PORTA |= (1 << PA0);
        DDRA |= (1 << PA0);

    } else {

        charger_a_disabled_by_i2c = false;
        charger_a_disabled_counter = 0;
		charger_a_finished_timestamp = 0xFFFFFFFF;
		
		/* Set CEA back to HiZ. */
        DDRA &= ~(1 << PA0);
        PORTA &= ~(1 << PA0);
    }
}

/* Disables or enables charger B through I2C. */
void set_charger_b_disabled(uint8_t is_disabled)
{
    if (is_disabled) {

        charger_b_disabled_by_i2c = true;
        charger_b_disabled_counter = 0;
		charger_b_finished_timestamp = 0xFFFFFFFF;

        /* Set CEB high. */
        PORTB |= (1 << PB1);
        DDRB |= (1 << PB1);

    } else {

        charger_b_disabled_counter = 0;
        charger_b_disabled_by_i2c = false;
		charger_b_finished_timestamp = 0xFFFFFFFF;

        /* Set CEB back to HiZ. */
        DDRB &= ~(1 << PB1);
        PORTB &= ~(1 << PB1);
    }
}

/* A callback triggered when the i2c master attempts to read from a register */
uint8_t i2c_read_from_register(uint8_t reg)
{
    switch (reg)
    {
        case 0: /* Magic number.identifying this expansion board */
            return I2C_SLAVE_ADDR;

        case 1: /* Version */
            return 1;

        case 2: /* State of charger A */
            return get_status_a();

        case 3: /* State of charger B */
            return get_status_b();

        case 4: /* Has charger A been disabled through I2C? */
            return charger_a_disabled_by_i2c ? 1 : 0;

        case 5: /* Has charger B been disabled through I2C? */
            return charger_b_disabled_by_i2c ? 1 : 0;

        default:
            return 0xff;
    }
}

/* A callback triggered when the i2c master attempts to write to a register */
void i2c_write_to_register(uint8_t reg, uint8_t value)
{
    switch (reg)
    {
        case 4: /* Disable or enable charger A */
            set_charger_a_disabled(value);
            break;

        case 5: /* Disable or enable charger B */
            set_charger_b_disabled(value);
            break;
    }
}


int main()
{
    /* Setup an interrupt to notice when charging stops. */
    MCUCR |= (1 << ISC10);  /* INT1 - trigger on any logic change */
    GIMSK |=
        (1 << INT1) |       /* Enable INT1 interupt */
        (1 << PCIE);        /* Enable PCINT change interrupt */
    PCMSK = (1 << PCINT3);  /* Trigger PCINT interrupt on PCINT3 */

    /* Setup the 16 bit timer to trigger every 1 second. */
    TCCR1B =
        (1 << CS12) | (1 << CS10) |     /* 1/1024 prescaler */
        (1 << WGM12);                   /* CTC mode */
    OCR1A = 7812;                       /* Set TOP of timer to give 1 second frequency */
    TIMSK = (1 << OCIE1A);              /* Enable the interrupt */

    usi_init(I2C_SLAVE_ADDR, i2c_read_from_register, i2c_write_to_register);

    sei();

    while (1)
    {
    }
}



/* This interrupt is used to detect and stop the following situation:
 * 1. Charging completes on the battery (STAT2 high, STAT1 low).
 * 2. The charger stops supplying power, and the battery now starts supplying power to the robot.
 * 3. If the circuit draw a sufficiently high current (eg > 500mA) this will drop the battery's voltage to below the charging threshold.
 * 4. Charger sees the voltage drop below the threshold and starts charging again (usuaully within a second of step 1).
 * This charging loop effectively results in charging not stopping if the circuit puts a high load on the battery. The logic below
 * captures the time when charging stops, then if the time when the next charge starts is too close, it disables charging for 10 minutes.
 */
ISR(INT1_vect)
{
	/* If PGA is off, ignore it */
	if (!(PIND & (1 << PD2))) {

		/* If the charger has just finished or is in an error state (ie STAT1 is off), record the time. */
		if (charger_a_is_charging && (PIND & (1 << PD3))) {
			charger_a_finished_timestamp = time_in_seconds;
		}
		/* Otherwise, if the charger has just started (ie STAT1 is on and STAT2 is off) */
		else if (!(PIND & (1 << PD3)) && (PIND & (1 << PD4))) {

			/* Check the charger has started charging within 1 minute of it finishing */
			if (charger_a_finished_timestamp != 0xFFFFFFFF && 
			    time_in_seconds - charger_a_finished_timestamp < 60) {

				/* Looks like it has, so disable the charger for 10 minutes. Pull CEA high. */
		        PORTA |= (1 << PA0);
		        DDRA |= (1 << PA0);

		        charger_a_disabled_counter = NUM_SECONDS_TO_DISABLE_AFTER_CHARGE;
			}
		}
	}
	charger_a_is_charging = (PIND & (1 << PD3)) == 0;
}

/* As above, but for supply B. */
ISR(PCINT_vect)
{
	/* If PGB is off, ignore it */
	if (!(PINB & (1 << PB2))) {

		/* If the charger has just finished or is in an error state (ie STAT1 is off), record the time. */
		if (charger_b_is_charging && (PINB & (1 << PB3))) {
			charger_b_finished_timestamp = time_in_seconds;
		}
		/* Otherwise, if the charger has just started (ie STAT1 is on and STAT2 is off) */
		else if (!(PINB & (1 << PB3)) && (PINB & (1 << PB4))) {

			/* Check the charger has started charging within 1 minute of it finishing */
			if (charger_b_finished_timestamp != 0xFFFFFFFF && 
			    time_in_seconds - charger_b_finished_timestamp < 60) {

				/* Looks like it has, so disable the charger for 10 minutes. Pull CEB high. */
		        PORTB |= (1 << PB1);
		        DDRB |= (1 << PB1);

	        	charger_b_disabled_counter = NUM_SECONDS_TO_DISABLE_AFTER_CHARGE;
			}
		}
	}
	charger_b_is_charging = (PINB & (1 << PB3)) == 0;
}

/* Triggered every second. Used to decrement the charger_(a|b)_disabled_counter variables. */
ISR(TIMER1_COMPA_vect)
{
	time_in_seconds++;

    if (charger_a_disabled_counter > 0)
    {
        charger_a_disabled_counter--;
        if (charger_a_disabled_counter == 0 && !charger_a_disabled_by_i2c) {
	        DDRA &= ~(1 << PA0);
	        PORTA &= ~(1 << PA0);
		}
    }

    if (charger_b_disabled_counter > 0)
    {
        charger_b_disabled_counter--;
        if (charger_b_disabled_counter == 0 && !charger_b_disabled_by_i2c) {
        	DDRB &= ~(1 << PB1);
        	PORTB &= ~(1 << PB1);
		}
    }

}
