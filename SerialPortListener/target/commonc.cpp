// 
// file common to both Arduino and standard avr boards
// 

#include "commonc.h"
#include "mySerial.h"

// variables used by ISR.
// cannot be part of C++ class bacused names get mangled and assembler won't find them
volatile uint16_t pc = 0;
volatile uint16_t targetPC = 0; 
volatile uint8_t saveR20;
volatile uint8_t saveR21;
volatile uint8_t saveR22;
volatile uint8_t saveR23;
char inData[10];

debugState dbState = BEGIN;

// ascii chars used for interaction strings
// can't use < 128 because normal Serial.print will use them
//typedef enum {PROGCOUNT_CHAR=248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR,YES_CHAR };


// prints 4-digit hex number
void printhex4(int num) {
	char snum[] = "0000";
	if (num > 0xFFF)
		itoa(num, snum, 16);
	else if (num > 0xFF)
		itoa(num, snum + 1, 16);
	else if (num > 0xF)
		itoa(num, snum + 2, 16);
	printnum((const char*)snum);

}

void readData(char* string, unsigned char maxlen) {
	unsigned char index = 0;
	int inChar;
	bool done = false;
	*string = 0;
	while (!done)
	{
		inChar = rchar();
		if (inChar > 0)
		{
			if (inChar == '\n' || inChar == '\r') {
				done = true;
			}
			else if (index < maxlen)
			{
				string[index] = inChar;
				index++;
				string[index] = '\0';
			}
		}
	}
}

void getValues() {
  unsigned int instr = 0;
	unsigned int* peekaddr = 0;
	unsigned int peeked = 0;
	readData(inData, 5);
  instr = (unsigned int)strtoul(inData + 1, NULL, 16);
	if (inData[0] == 'A') {
		//peekaddr = (unsigned int*)strtoul(inData + 1, NULL, 16);
		peeked = *(unsigned int*)instr;
		//printc('A'); printhex4((int)peekaddr);
		printc('D'); printhex4(peeked); println("");
    return;
	}
  if (inData[0] == 'P') {
    if (instr == 0) {
      dbState = SINGLESTEP;
    }
    else { // new breakpoint
      targetPC = instr >> 1;
      printc('T');  printhex4(instr); println(""); // may not need this line when debugged ********
      dbState = BPSEARCH;
    }
  }
}


debugState getInstructions() {
	unsigned int instr = 0;
	printc('P');  printhex4(pc * 2); printc('?'); println(""); 
	readData(inData, 5);
	if (inData[0] == 'P') {
		instr = (unsigned int)strtoul(inData + 1, NULL, 16);
		if (instr == 0xFFFF) {
			// special case, want to inspect variables
			dbState = VALUES;
			while (dbState == VALUES)
				getValues();
      return dbState;
		}
		else if (instr == 0)
		{
			// not at a breakpoint, just continue....
			return SINGLESTEP;
		}
		else {
			// need to reach a source line, i.e. pc needs to be on one of the breakpoint objects
			targetPC = instr >> 1;
			printc('T');  printhex4(instr); println(""); // may not need this line when debugged ********
      return BPSEARCH;
		}
	}
  return dbState;
}

char readAnswer()
{
  // just get a  simple Y or N from the desktop app
  int inChar;
  while (1)
  {
    inChar = rchar();
    if (inChar == 'Y' || inChar == 'N') {
        return inChar;
    }
  }
}
bool areWeAtALine()
{
  // indicate to desktop that we are single-stepping;
  // does this address equate to any in the potential breakpoint list (of source code lines?)
  printc('S');  printhex4(pc * 2); printc('?'); println(""); 
  char c = readAnswer();
  return (c == 'Y');
}

//needs to be extern "c" so the assembler can find it (C++ mangles function names)
extern "C"
{
	__attribute__((used)) void checkhit()  {
		unsigned int changeTarget = 0;

		switch (dbState) {
		case BEGIN:
			// need to get pc for setup() from desktop app
			getInstructions();
			// now run until we get there
			dbState = SINGLESTEP;
			break;
		case SINGLESTEP:
    	// just do whatever the program wants....continue until we reach an instruction that matches a source code line
            // need to get pc for next source code line from desktop app
        if (areWeAtALine())
        {
                  dbState = JUSTSTEPPED;
                  dbState = getInstructions();

        }
   			break;
     case BPSEARCH:
      if (pc == targetPC) {
                  dbState = JUSTSTEPPED;
                  dbState =getInstructions();

		  }
      break;
		}
	}
}

