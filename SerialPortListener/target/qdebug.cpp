/*
  QDebug.cpp - Library for basic software debugging an Arduino sketch
  Created by Chris Fearnley, March 2017
  Released into the public domain.
*/

#include "qdebug.h"
#include "mySerial.h"

QDebug::QDebug()
{

	Serial.begin(57600);
	delay(500);

  // force continuous interrupts on analog compare pin
	pinMode(PD7, OUTPUT);
	ACSR = (1 << ACBG) | (1 << ACIE);
	sei();
}



