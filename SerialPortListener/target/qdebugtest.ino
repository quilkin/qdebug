#include "qdebug.h"

void setup() {
  QDebug qdebug;

}
unsigned long ms = 0;
int num = 1;
unsigned int numui = 0;
float numf = 0.1;
char chchch = '\0'; 

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
  numui = 44;
  numf = 3.45;
  num = 66;
  chchch = 'a';
  if (millis() - ms > 60)
  {
    numui += 1;
    num += 2;
    numui += 3;
    num += 4;
    num = incInt(num);
    numui += 6;
    num += 7;
    numf += 0.1;
    num += 8;
    num = doubleInt(num);
    num += 10;
    chchch = 'b';
    numui = peeky((unsigned int*)0x100);
    ms = millis();
  }
}
