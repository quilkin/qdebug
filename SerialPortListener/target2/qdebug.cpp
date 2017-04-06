/*
  QDebug.cpp - Library for basic software debugging an Arduino sketch
  Created by Chris Fearnley, March 2017
  Released into the public domain.
*/

#include "qdebug.h"
#include "gdb.h"

__attribute__((optimize("-O0")))
void sticker(void)
{
	while (1);
}

__attribute__((optimize("-O0")))
Qdebug::Qdebug()
{
    debug_init();
  // force continuous interrupts on analog compare pin
	pinMode(PD7, OUTPUT);
	ACSR = (1 << ACBG) | (1 << ACIE);
	sei();
	// wait here as an initial 'breakpoint'
	sticker();
}



