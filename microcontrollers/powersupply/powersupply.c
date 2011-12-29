/*
 *  powersupply.c
 *
 *  Exposes the charging state of two BQ24123 LiPo charger ICs over and I2C interface.
 *
 *  Copyright (C) Ben Galvin 2012
 *
 *  Feel free to do whatever you want with the code. Use at your own risk.
 *
 *  Microcontroller Pin Setup
 *  =========================
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
#include <util/delay_basic.h>
#include <stdbool.h>
#include "../common/usi_slave.h"


#define I2C_SLAVE_ADDR  0x30
#define NUM_SECONDS_TO_DISABLE_AFTER_CHARGE (10*60)


/* Globals */
bool charger_a_disabled_by_i2c = false;
volatile uint16_t charger_a_disabled_counter = 0;

bool charger_b_disabled_by_i2c = false;
volatile uint16_t charger_b_disabled_counter = 0;


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

    return status;
}

/* Disables or enables charger A. */
void set_charger_a_disabled(uint8_t is_disabled)
{
    if (is_disabled)
    {
        /* Set the pin high. */
        PORTA |= (1 << PA0);
        DDRA |= (1 << PA0);
        charger_a_disabled_by_i2c = true;
        charger_a_disabled_counter = 0;
    }
    else
    {
        /* Set it back to HiZ. */
        DDRA &= ~(1 << PA0);
        PORTA &= ~(1 << PA0);
        charger_a_disabled_counter = 0;
        charger_a_disabled_by_i2c = false;
    }

}

/* Disables or enables charger B. */
void set_charger_b_disabled(uint8_t is_disabled)
{
    if (is_disabled)
    {
        /* Set the pin high. */
        PORTB |= (1 << PB1);
        DDRB |= (1 << PB1);
        charger_b_disabled_by_i2c = true;
        charger_b_disabled_counter = 0;
    }
    else
        {
        /* Set it back to HiZ. */
        DDRB &= ~(1 << PB1);
        PORTB &= ~(1 << PB1);
        charger_b_disabled_counter = 0;
        charger_b_disabled_by_i2c = false;
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



/* See if charging has just completed for supply A. If it has, prevent recharging from starting for another
 * 10 minutes. Without this if their is significant system load it will pull the battery voltage down enough
 * to trigger another charging cycle within a second of charging finishing.
 */
ISR(INT1_vect)
{
    /* If STAT2 is on and STAT1 is off, charging has just completed. */
    if (!(PIND & (1 << PD4)) && (PIND & (1 << PD3)))
    {
        /* Pull CEA low */
        PORTA |= (1 << PA0);
        DDRA |= (1 << PA0);

        charger_a_disabled_counter = NUM_SECONDS_TO_DISABLE_AFTER_CHARGE;
    }
}

/* As above, but for supply B. */
ISR(PCINT_vect)
{
    if (!(PINB & (1 << PB4)) && (PINB & (1 << PB3)))
    {
        // Pull CEB low.
        PORTB |= (1 << PB1);
        DDRB |= (1 << PB1);

        charger_b_disabled_counter = NUM_SECONDS_TO_DISABLE_AFTER_CHARGE;
    }
}

/* Triggered every second. Used to decrement the charger_(a|b)_disabled_counter variables. */
ISR(TIMER1_COMPA_vect)
{

    if (charger_a_disabled_counter > 0)
    {
        charger_a_disabled_counter--;
        if (charger_a_disabled_counter == 0 && charger_a_disabled_by_i2c)
            set_charger_a_disabled(0);
    }

    if (charger_b_disabled_counter > 0)
    {
        charger_b_disabled_counter--;
        if (charger_b_disabled_counter == 0 && charger_b_disabled_by_i2c)
            set_charger_b_disabled(0);
    }

}
