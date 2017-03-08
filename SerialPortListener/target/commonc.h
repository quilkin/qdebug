// common_c.h

#ifndef _COMMONC_h
#define _COMMONC_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif
#include "mySerial.h"

typedef enum { BEGIN, SINGLESTEP, JUSTSTEPPED, BPSEARCH, VALUES } debugState;
void printhex4(int num);
void readData(char* string, unsigned char maxlen);
void getValues();
debugState  getInstructions();
//void checkHit() __attribute__((used));
//void checkhit() ;

#endif

