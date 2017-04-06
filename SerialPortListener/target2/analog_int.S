/*
 * analog_int.S
 *
 * Created: 23/02/2017 15:09:09
 *  Author: chris
 */ 
 #include <avr/io.h>
 #define __GDB__ 
 #define __LargeStub__

    .global ANALOG_COMP_vect
   __SP_H__ = 0x3e
   __SP_L__ = 0x3d
   __SREG__ = 0x3f

   ANALOG_COMP_vect:

 #ifdef __LargeStub__

	sts	regs+31, r31		/* save R31 */
	sts	regs+30, r30		/* save R30 */
	ldi	r31, hi8(regs)	/* Z points to regs */
	ldi	r30, lo8(regs)
	std	Z+29, r29		/* save R29 */
	in	r29, __SREG__	/* get SREG */
	//ori r29, 0x80			/* user must see interrupts enabled */

	std	Z+32, r29		/* put SREG value to his please */
	std	Z+28, r28		/* save R28 */
	ldi	r29, 0		/* Y points to 0 */
	ldi	r28, 0

	/* R0 address is 0; this loop saves from 0 to Z, which is
	 a "pointer" to "regs" variable .	 */
	std	Z+27, r27		/* save R27 */
1:	ld	r27, Y+		/* load register 0..26 */
	st	Z+, r27		/* save it */
	cpi	r28, 27
	brne	1b
#if defined(__AVR_ATmega2560__)
	pop 	r25			/* pop return address, high-high byte for AVRs with 3-byte PC */
#endif
	pop	r27			/* pop return address, high byte */
	pop	r26			/* pop return address, low byte */

	/* now save the return address - where we will really return */
1:	 std	Z+35-27, r26		/* save return address (PC) */
	std	Z+36-27, r27		/* -27 because from previous code Z points to regs+27*/
#if defined(__AVR_ATmega2560__)
	std	Z+37-27, r25
#endif
	in	r26, __SP_L__
	in	r27, __SP_H__
	std	Z+33-27, r26		/* save SP */
	std	Z+34-27, r27
	clr	r1

	call gdb_break
		// force immediate analog compare interrupt again (as soon as this one is done)
    ldi   r24, 0x80
    out   0x09, r24
	// restore registers
	ldi	r31, hi8(regs)	/* Z points to regs */
	ldi	r30, lo8(regs)
	ldi	r29, 0		/* Y points to 0 */
	ldi	r28, 0
1:	ld	r27, Z+
	st	Y+, r27		/* restore register 0..27 */
	cpi	r28, 28
	brne	1b

	ldd	r29, Z+33-28		/* SP low */
	out	__SP_L__, r29
	ldd	r29, Z+34-28		/* SP high */
	out	__SP_H__, r29
	ldd	r29, Z+35-28		/* PC low */
	push	r29
	ldd	r29, Z+36-28		/* PC high */
	push	r29
#if defined(__AVR_ATmega2560__)
	ldd	r29, Z+37-28		/* PC high-high */
	push	r29
#endif
	ldd	r28, Z+28-28		/* restore R28 */
	ldd	r29, Z+29-28		/* restore R29 */
	ldd	r30, Z+30-28		/* restore R30 */
	lds	r31, regs+32;	/* r31 = sreg */
//	call restore_regs

//	sbrs	r31, 7		/* test I flag */
//	rjmp	1f
	andi	r31, 0x7f		/* clear I flag */
	out	__SREG__, r31	/* restore SREG */
	lds	r31, regs+31		/* real value of r31 */
	reti			/* exit with interrupts enabled */
//1:	out	__SREG__, r31	/* exit with interrupts disabled */
//	lds	r31, regs+31	/* real value of r31 */

 #else
 
   ///////////////////////  save all registers 18-27,30,31: they may be used by functions (and we need to find them for local vars)
   ////////////////////// see http://www.atmel.com/webdoc/AVRLibcReferenceManual/FAQ_1faq_reg_usage.html
   //////////// Also need to save R28,29  because they will be used to access local vars
    push  r1
    push  r0
    in    r0, 0x3f
    push  r0
    eor   r1, r1

    push  r31 
    push  r30 
	push  r29
	push  r28
    push  r27
    push  r26
    push  r25
    push  r24
	push  r23
	push  r22
	push  r21
	push  r20
    push  r19
    push  r18

     // get the current frame pointer and program counter
    in    r30, __SP_L__  // Temporary frame 
    in    r31, __SP_H__ 
    ldd   r24,Z+18      
    ldd   r25,Z+19    
    sts   pc+1, r24 
    sts   pc, r25 
	sts   frame+1, r31 
    sts   frame, r30

    // allow checkhit to use interrupts 
    sei
	#ifdef __GDB__
	//call  gdb_stepping
	call gdb_break
	#else
    call  checkhit
	#endif
    cli

    // force immediate interrupt again (as soon as this one is done)
    ldi   r24, 0x80
    out   0x09, r24

    pop r18
    pop r19
	pop r20
    pop r21
    pop r22
    pop r23
    pop r24
    pop r25
    pop r26
    pop r27
	pop r28
	pop r29
    pop r30
    pop r31

    pop r0
    out 0x3f, r0
    pop r0
    pop r1
    reti 

#endif // not large stub