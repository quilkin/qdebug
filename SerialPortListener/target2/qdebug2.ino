#include "qdebug2.h"
#include "gdb.h"

Qdebug mydebug;

int myInt;
int myInt2;
int *myPointer;
long *myLongPointer;
int myArray[2];
long myLongArray[3];

int __attribute__((optimize("O0"))) myAdd(int a, int b, int c) {
	int index;
	for (index = 0; index < 5; index++)
	{
		a += b;
		a += c;
	}
	return a;
}

int  __attribute__((optimize("O0"))) myNull(void) {
	int index;
	int a = 1, b = 2, c = 3;
	for (index = 0; index < 5; index++)
	{
		a += b;
		a += c;
	}
	return a;
}
void  __attribute__((optimize("O0"))) setup() {
	
	myInt = 1;
	myInt2 = 2;

	myPointer = &myInt;
	//myLongPointer =myLongArray;
	myInt2 = *myPointer;
}
void loop() {
	++myInt;
	++myArray[1];
	myInt = myNull();
	myInt = myAdd(myInt, 5, myInt2);

	++myLongArray[0];
}
