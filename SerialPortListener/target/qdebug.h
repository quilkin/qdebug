// qdebug.h

#ifndef _QDEBUG_h
#define _QDEBUG_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

// variables used by ISR.
// cannot be part of C++ class bacused names get mangled and assembler won't find them
extern volatile uint16_t pc, targetPC; 

class QDebug 
{
  public:
	QDebug();
	void SetPcTarget(unsigned int target);

};

extern "C" {
  extern void checkHit(void)__attribute__((used));
}
// reduce normal optimisation (otherwise function calls will be optimised out)
#pragma GCC optimize ("O0")
#endif

