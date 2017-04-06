/*
 * serial_gdb.c
 *
 * Created: 31/03/2017 19:49:52
 *  Author: chris
 */ 

 #include <avr/io.h>
 #include <avr/pgmspace.h>
 #include <stdio.h>
 #include <stddef.h>
 #include <string.h>

 /* Serial port baudrate */
/* Note that we need to use the double UART speed option (U2X0 bit = 1) for the 115200 baudrate on Uno.
 * Use the double speed always! For Arduino Mega it has lower error both for 57600 and 115200 */
/* For Arduino Mega 1280 there is error in baud 2.1% for 115200. For 57600 the error is -0,8%.
 * Debugging seems to work better (sometimes only) for 57600.  */
#if defined(__AVR_ATmega1280__)
	/* For ATmega1280 baudrate the debugger communicates at 57600... */
	#define GDB_USART_BAUDRATE 57600
#else
	#define GDB_USART_BAUDRATE 115200
#endif
#define F_CPU 16000000L

/* For double UART speed (U2X0 bit = 1) use this macro: */
#define GDB_BAUD_PRESCALE (((( F_CPU / 8) + ( GDB_USART_BAUDRATE / 2) ) / ( GDB_USART_BAUDRATE )) - 1)

void uart_init(void)
{
	/* Init UART */
	UCSR0A = _BV(U2X0);		/* double UART speed */
	UCSR0B = (1 << RXEN0 ) | (1 << TXEN0 );		/* enable RX and Tx */
	UCSR0C =  (1 << UCSZ00 ) | (1 << UCSZ01 ); /* Use 8- bit character sizes */
	//UBRR0H = ( GDB_BAUD_PRESCALE >> 8) ;
	//UBRR0L = GDB_BAUD_PRESCALE ;
	// from http://wormfood.net/avrbaudcalc.php for 16 MHz, 115200 baud
	UBRR0H = 0;
	//UBRR0L =0x22 // 57600
	//UBRR0L =0x10 ; // 115200
	UBRR0L =0x07 ; // 250000
//	UCSR0B |= (1 << RXCIE0 ); /* Enable the USART Recieve Complete interrupt ( USART_RXC ) */
//unsigned long baud = 57600;
//
//// method copied from Arduino HardwareSerial
 //// Try u2x mode first
 //uint16_t baud_setting = (F_CPU / 4 / baud - 1) / 2;
 //UCSR0A = 1 << U2X0;
//
 //// hardcoded exception for 57600 for compatibility with the bootloader
 //// shipped with the Duemilanove and previous boards and the firmware
 //// on the 8U2 on the Uno and Mega 2560. Also, The baud_setting cannot
 //// be > 4095, so switch back to non-u2x mode if the baud rate is too
 //// low.
 //if (((F_CPU == 16000000UL) && (baud == 57600)) || (baud_setting >4095))
 //{
	 //UCSR0A = 0;
	 //baud_setting  = (F_CPU / 8 / baud - 1) / 2;
 //}
//
 //// assign the baud_setting, a.k.a. ubrr (USART Baud Rate Register)
 //UBRR0H = baud_setting >> 8;
 //UBRR0L = baud_setting;
}

void delay_us(uint16_t delay) {
	volatile uint16_t i = 0;
	delay >>= 1;
	for (i = 0; i < delay; i++) {
		__asm__ __volatile__ ("nop");
	}
}

/* Read a single character from the serial port 
   Waits 100uS approx before giving up */
uint8_t getDebugChar(void)
{
	uint8_t count = 0;
	/* wait for data to arrive */
	while ( !(UCSR0A & (1<<RXC0)) )
	{
		//if (++ count > 50)
			//return 0;
		//delay_us(100);
	}
	return (uint8_t)UDR0;
}

// see if any data is there
uint8_t checkDebugChar(void)
{
	return (UCSR0A & (1<<RXC0)) ;
}

/* Write a single character to serial port */
void putDebugChar(uint8_t c)
{
	/* Wait for empty transmit buffer */
	while ( !( UCSR0A & (1<<UDRE0)) )
	;

	/* Put data into buffer, sends the data */
	UDR0 = c;
}
