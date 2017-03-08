#include "qdebug.h"

int num ;
float numf;
float numfn;
unsigned int numui ;
char chchch ; 
unsigned char bytebyte ;
unsigned long ms ;
unsigned long ms2 ;
int anotherInt;


void setup() {
  QDebug qdebug;
  ms2 = 100000L;
  ms = 0;
  numf = 12.345;
  numfn = -5.67E-10;
  num = 66;
  numui = 44;
  chchch = 'a';
  bytebyte = 200;
 anotherInt = 345;
}

int incInt(int iii) {
  int locali = iii*3;
  return iii + locali;
}
int doubleInt(int i) {
  return i*2;
}
unsigned int peeky (unsigned int* peeker)
{
  return *peeker;
}



void loop() {

  if (millis() - ms > 60)
  {
    numui += 1;
    num += 2;
    numui += 3;
    num += 4;
    num = incInt(num);
    numui += 6;
    num += 7;
    numf *= 100;
    num += 8;
    num = doubleInt(num);
    num += 10;
    chchch = 'b';
    numui = peeky((unsigned int*)0x100);
    ms = millis();
  }
}
