// mySerial.h

#ifndef _MYSERIAL_h
#define _MYSERIAL_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

// just to isolate serial routines to keep them out of common files.
// This enables common files to be used in Arduino & non-arduino without change


	void printnum(const char* snum);
	int rchar();
	void printc(char c);
	int printstr(const char* string);
	int println(const char* string);


#endif

