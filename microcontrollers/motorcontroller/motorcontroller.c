/*
 *  motorcontroller.c
 *
 *  Uses an ATTiny2313 to drive two DC motors via an L298 dual full bridge IC. The
 *  motors direction and speed are exposed over an I2C interface.
 *
 *  Copyright (C) Ben Galvin 2012
 *
 *  Feel free to do whatever you want with the code. Use at your own risk.
 *
 *  Microcontroller Pin Setup
 *  =========================
 *  Pin2/PD0: 		IN4 Motor B
 *  Pin3/PD1: 		IN3 Motor B
 *  Pin4/PA1: 		IN2 Motor A
 *  Pin5/PA0: 		IN1 Motor A
 *  Pin12/PB0: 		Quadrature B Pin 3
 *  Pin13/PB1: 		Quadrature B Pin 4
 *  Pin14/PB2: 		Quadrature A Pin 3
 *  Pin15/PB3/OC1A: Enable A
 *  Pin16/PB4/OC1B: Enable B
 *  Pin18/PB6: 		Quadrature A Pin 4
 *
 */

#include <avr/io.h>
#include <avr/interrupt.h>
#include <util/delay.h>
#include "../common/usi_slave.h"


#define I2C_SLAVE_ADDR  0x10


/* Globals */

uint8_t motor_a_state = 0;
uint8_t motor_b_state = 0;


void set_motor_a_state(uint8_t value)
{
	motor_a_state = value;

	switch (value) {

		case 0: /* Free running */
			PORTB &= ~(1 << PB3);
			TCCR1A &=~ (1 << COM1A1);
			PORTA &= ~(1 << PA0) | (1 << PA1);
			break;

		case 1: /* Forwards */
			TCCR1A |= (1 << COM1A1);
			PORTA |= (1 << PA0);
			PORTA &= ~(1 << PA1);
			PORTB |= (1 << PB3);
			break;

		case 2: /* Reverse */
			TCCR1A |= (1 << COM1A1);
			PORTA &= ~(1 << PA0);
			PORTA |= (1 << PA1);
			PORTB |= (1 << PB3);
			break;

		case 3: /* Brake */
			TCCR1A |= (1 << COM1A1);
			PORTA |= (1 << PA0) | (1 << PA1);
			PORTB |= (1 << PB3);
			break;
	}
}

void set_motor_b_state(uint8_t value)
{
	motor_b_state = value;

	switch (value) {

		case 0: /* Free running */
			PORTB &= ~(1 << PB4);
			TCCR1A &=~ (1 << COM1B1);
			PORTD &= ~(1 << PD0) | (1 << PD1);
			break;

		case 1: /* Forwards */
			TCCR1A |= (1 << COM1B1);
			PORTD |= (1 << PD1);
			PORTD &= ~(1 << PD0);
			PORTB |= (1 << PB4);
			break;

		case 2: /* Reverse */
			TCCR1A |= (1 << COM1B1);
			PORTD &= ~(1 << PD1);
			PORTD |= (1 << PD0);
			PORTB |= (1 << PB4);
			break;

		case 3: /* Brake */
			TCCR1A |= (1 << COM1B1);
			PORTD = (1 << PD0) | (1 << PD1);
			PORTB |= (1 << PB4);
			break;
	}
}

/* A callback triggered when the i2c master attempts to read from a register.*/
uint8_t i2cReadFromRegister(uint8_t reg)
{
	switch (reg)
	{		
		case 0: /* Magic number.identifying this expansion board. */
			return I2C_SLAVE_ADDR;

		case 1: /* Version */
			return 1;

		case 2: /* State of motor A */
			return motor_a_state;

		case 3: /* Speed of motor A */
			return OCR1AL;

		case 4: /* State of motor B */
			return motor_b_state;

		case 5: /* Speed of motor B */
			return OCR1BL;

		case 6: /* Read quadrature for engine A */
			return 0;

		case 7: /* Read quadrature for engine B */
			return 0;

		default:
			return 0xFF;

	}
}

/* A callback triggered when the i2c master attempts to write to a register. */
void i2cWriteToRegister(uint8_t reg, uint8_t value)
{
	switch (reg)
	{		
		case 2: /* State of motor A: 0 = Free Running, 1 = Forwards, 2 = Reverse, 3 = Fast Motor Stop */
			set_motor_a_state(value);
			break;

		case 3: /* Speed of engine A */
			OCR1AH = 0;
			OCR1AL = value;
			break;

		case 4: /* State of motor B:  0 = Free Running, 1 = Forwards, 2 = Reverse, 3 = Fast Motor Stop */
			set_motor_b_state(value);
			break;

		case 5: /* Speed of motor B */
			OCR1BH = 0;
			OCR1BL = value;
			break;
	}
}

int main()
{
	/* Setup timer */
	TCCR1A = 
		(1 << COM1A1) | 	/* Clear 0C1A on Compare Match */
		(1 << COM1B1) | 	/* Clear 0C1B on Compare Match */
		(1 << WGM10); 		/* Fast PWM, 8bit, 0x00FF TOP */

	TCCR1B =
		(1 << CS11) | (1 << CS10) | 	/* clk/64 prescaler */
		(1 << WGM12); 				    /* Fast PWM, 8bit, 0x00FF TOP */

	/* Engines off initially. */
	OCR1AH = 0;
	OCR1AL = 0;
	OCR1BH = 0;
	OCR1BL = 0;
	
	DDRB = 
		(1 << PB3) | 	/* Set Pin15/PB3/OC1A - Enable A as output. */
		(1 << PB4);  	/* Set Pin16/PB4/OC1B - Enable B as output. */

	DDRD = 
		(1 << PD0) | 	/* Set Pin2/PD0 - IN4 Motor B */
		(1 << PD1);	 	/* Set Pin3/PD1 - IN3 Motor B */

	DDRA = 
		(1 << PA0) | 	/* Set Pin4/PA1 - IN2 Motor A as output */
		(1 << PA1);  	/* Set Pin5/PA0 - IN1 Motor A as output */

	usi_init(I2C_SLAVE_ADDR, i2cReadFromRegister, i2cWriteToRegister);

	sei();

	/*// Setup external interrupt on PCINT7
	GIMSK |= 1 << PCIE1;
	PCMSK0 = 1 << PCINT7;
	PCMSK1 = 0;
	GIFR = 1 << PCIF;
	sei();
	*/

	//MCUCR |= 1 << ISC01 | 1 << ISC00;

	while (1)
	{
	}
}


/*ISR(INT0_vect) 
{
	switch (x) 
	{
		case 0: 
			PORTB |= (1 << PB4);
			break;
		case 1: 
			PORTB &= ~(1 << PB4);
			PORTB |= (1 << PB5); 
			break;
		case 2:
			PORTB &= ~(1 << PB5);
			x = 0;
			return;
	}
	x++;
}*/

/*
ISR(PCINT_vect) 
{
}*/
