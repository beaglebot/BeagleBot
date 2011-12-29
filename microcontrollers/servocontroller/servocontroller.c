/*
 *  servocontroller.c
 *
 *  Drives 2 analog servos using an ATTiny2313 microcontroller and exposes the servo parameters over an I2C interface.
 *
 *  Copyright (C) Ben Galvin 2012
 *
 *  Feel free to do whatever you want with the code. Use at your own risk.
 *
 *  Microcontroller Pin Setup
 *  =========================
 *  Pin 16/PB4/OC1B: Servo 1 output
 *  Pin 15/PB3/OC1A: Servo 2 output
 *
 */

#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/delay.h>
#include "../common/usi_slave.h"


#define I2C_SLAVE_ADDR  0x20
#define SERVO_A_PIN PB3
#define SERVO_B_PIN PB4
#define INITIAL_PULSE_WIDTH ((600+2400)/2)


/* Globals */
volatile uint8_t timer_a_enable, timer_a_high_byte;
volatile uint8_t timer_b_enable, timer_b_high_byte;


void set_pulse_width_timer_a(uint8_t high, uint8_t low)
{
	OCR1AH = high;
	OCR1AL = low;
}

void set_pulse_width_timer_b(uint8_t high, uint8_t low)
{
	OCR1BH = high;
	OCR1BL = low;
}

/* A callback triggered when the i2c master attempts to read from a register. */
uint8_t i2c_read_from_register(uint8_t reg)
{
	switch (reg)
	{
		case 0: /* Magic number.identifying this expansion board. */
			return I2C_SLAVE_ADDR;

		case 1: /* Version */ 
			return 1;

		case 2: /* Servo A status */
			return (DDRB & (1 << SERVO_A_PIN)) != 0;

		case 3: /* Servo A pulse width high byte. This must be read *after* the low byte */
			return timer_a_high_byte;

		case 4: /* Servo B pulse width low byte. */
			timer_a_high_byte = OCR1AH;
			return OCR1AL;

		case 5: /* Servo B status */
			return (DDRB & (1 << SERVO_B_PIN)) != 0;

		case 6: /* Servo B pulse width high byte. This must be read *before* the low byte */
			return timer_b_high_byte;

		case 7: /* Servo B pulse width low byte. */
			timer_b_high_byte = OCR1BH;
			return OCR1BL;

		default:
			return 0xFF;
	}
}

/* A callback triggered when the i2c master attempts to write to a register. */
void i2c_write_to_register(uint8_t reg, uint8_t value)
{
	switch (reg)
	{
		case 2: /* Servo A status */
			timer_a_enable = value;
			if (timer_a_enable) DDRB |= (1 << SERVO_A_PIN); else DDRB &= ~(1 << SERVO_A_PIN);
			break;

		case 3: /* Servo A pulse width high byte. This must be written *before* the low byte. */
			timer_a_high_byte = value;
			break;

		case 4: /* Servo A pulse width low byte */
			set_pulse_width_timer_a(timer_a_high_byte, value);
			break;

		case 5: /* Servo B status */
			timer_b_enable = value;
			if (timer_b_enable) DDRB |= (1 << SERVO_B_PIN); else DDRB &= ~(1 << SERVO_B_PIN);
			break;

		case 6: /* Servo B pulse width high byte. This must be written *before* the low byte. */
			timer_b_high_byte = value;
			break;

		case 7: /* Servo B pulse width low byte. */
			set_pulse_width_timer_b(timer_b_high_byte, value);
			break;
	}
}

int main()
{
	/* Initially don't send a signal to the servos. */
	DDRB = 0; 
	DDRD |= (1 << PD5);
		
	TCCR1A = 
		(1 << COM1A1) | 	/* Clear 0C1A on Compare Match */
		(1 << COM1B1) | 	/* Clear 0C1B on Compare Match */
		(1 << WGM11); 		/* Fast PWM */
	TCCR1B = 
		(1 << WGM13) | (1 << WGM12) | 	/* Fast PWM */
		(1 << CS11); 				  	/* 1/8 Prescaler */
	
	ICR1H = 0x3a; ICR1L = 0x98; /* Set the TOP at 15ms */

	/* Initialize the servos to half way */
	set_pulse_width_timer_a(INITIAL_PULSE_WIDTH >> 8, INITIAL_PULSE_WIDTH & 0xFF);
	set_pulse_width_timer_b(INITIAL_PULSE_WIDTH >> 8, INITIAL_PULSE_WIDTH & 0xFF);

	usi_init(I2C_SLAVE_ADDR, i2c_read_from_register, i2c_write_to_register);
	sei();

	while (1) 
	{
	}
}
