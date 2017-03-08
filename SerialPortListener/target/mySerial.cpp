// 
// 
// 

#include "mySerial.h"

// todo: make these inline?



	void printnum(const char* snum) {
		Serial.print(snum);
	}
	int rchar() {
		return Serial.read();
	}
	void printc(char c) {
		Serial.print(c);
	}
	int printstr(const char* string) {
		Serial.print(string);
	}
	int println(const char* string) {
		Serial.print(string);
   // don't want to send '\r' as well
   Serial.print('\n');
	}

