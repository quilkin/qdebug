// qdebug.h

#ifndef _QDEBUG_h
#define _QDEBUG_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#if !defined(__AVR_ATmega328P__) 

//#error : Qdebug cannot be used with this board - only AtMega328 devices (Uno, Nano, Mini) are supported in this version

#endif 
// variables used by ISR.
// cannot be part of C++ class bacused names get mangled and assembler won't find them
extern volatile uint16_t pc, targetPC; 

class Qdebug 
{
  public:
	Qdebug();
};

//extern "C" {
//  extern void checkHit(void)__attribute__((used));
//}
// reduce normal optimisation (otherwise function calls will be optimised out)
#pragma GCC optimize ("O0")
#endif

