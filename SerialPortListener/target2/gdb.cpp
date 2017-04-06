/*
 * gdb.c
 *
 * Created: 27/03/2017 19:50:00
 *  Author: chris
 *
 *  bits stolen from avr8-stub.c
 *      Author: Jan Dolinay
 */ 
 #include "gdb.h"

    volatile uint16_t pc = 0;
    volatile uint16_t targetPC = 0;
    volatile uint16_t frame = 0;
	struct gdb_context ctx;
	struct gdb_context *gdb_ctx;


/* Initialize the debug driver. */
void debug_init(void)
{
	/* Init gdb context */
	gdb_ctx = &ctx;
	gdb_ctx->sp = 0;
#if defined(__LargeStub__)
	gdb_ctx->breaks_cnt = 0;
	/* Init breaks */
	memset(gdb_ctx->breaks, 0, sizeof(gdb_ctx->breaks));
#else
	gdb_ctx->break0 = 0;
	gdb_ctx->break1 = 0;
#endif
	gdb_ctx->buff_sz = 0;

	//gdb_ctx->singlestep_enabled = 0;
	//gdb_ctx->breakpoint_enabled = 0;

	/* Initialize serial port */
	uart_init();
}

 extern volatile uint16_t pc;
 extern volatile uint16_t frame;
//
//// called analog interrupt every instruction 
//void gdb_stepping() {
//
//// copy values from ISR
	//gdb_ctx->pc = pc;
	//gdb_ctx->sp = frame;
//
	//if ( gdb_ctx->singlestep_enabled)
		//goto trap;
	//
	//if ( gdb_ctx->breakpoint_enabled )
	//{
//#if defined(__LargeStub__)
		//uint8_t ind_bks;
		//for (ind_bks = 0; ind_bks < gdb_ctx->breaks_cnt; ind_bks++)
		//{
			//if (gdb_ctx->breaks[ind_bks] == gdb_ctx->pc) {
				//goto trap;
			//}
		//}
//#else
		//if (gdb_ctx->break0 == gdb_ctx->pc)
			//goto trap;
		//if (gdb_ctx->break1 == gdb_ctx->pc)
		//goto trap;
//#endif
	//}
	//return;
	//trap:
		//gdb_send_state(GDB_SIGTRAP);
		//gdb_stepping();
//}
//needs to be extern "c" so the assembler can find it (C++ mangles function names)
extern "C"
{
// called from from uart ISR if server sends CTRL C
__attribute__((optimize("-O0")))
 void gdb_break() {
 	uint8_t checksum, pkt_checksum;
	uint8_t b;

		#if defined(__AVR_ATmega2560__)
		R_PC_HH &= 0x01;		/* there is only 1 bit used in the highest byte of PC (17-bit PC) */
		/* No need to mask R_PC_H */
		#else
		R_PC_H &= RET_ADDR_MASK;
		#endif
	// copy values from ISR

#ifdef __LargeStub__
	gdb_ctx->pc = R_PC;
	gdb_ctx->sp = R_SP;
#else
	gdb_ctx->pc = pc;
	gdb_ctx->sp = frame;
#endif
	//// stop if singlestep point or breakpoint
	//if ( gdb_ctx->singlestep_enabled)
		//goto trap;
		
	//if ( gdb_ctx->breakpoint_enabled )
	//{
#if defined(__LargeStub__)
		uint8_t ind_bks;
		for (ind_bks = 0; ind_bks < gdb_ctx->breaks_cnt; ind_bks++)
		{
			if (gdb_ctx->breaks[ind_bks] == gdb_ctx->pc) {
				goto trap;
			}
		}
#else
		if ((gdb_ctx->break0 == gdb_ctx->pc )||  (gdb_ctx->break1 == gdb_ctx->pc))
			goto trap;
#endif
	//}
	// stop if message from server
	if (checkDebugChar()==0)
		return;
	//int bb = getchar();	// Todo: ****** faster version of this????
	//if (bb < 0)
		//return;
	//b = bb;

	while (1) {
		b = getDebugChar();
		if (b==0)
			return;

		switch(b) {
		case '$':
			/* Read everything to buffer */
			gdb_ctx->buff_sz = 0;
			for (pkt_checksum = 0, b = getDebugChar();
				 b != '#'; b = getDebugChar())
			{
				gdb_ctx->buff[gdb_ctx->buff_sz++] = b;
				pkt_checksum += b;
			}
			gdb_ctx->buff[gdb_ctx->buff_sz] = 0;

			checksum  = hex2nib(getDebugChar()) << 4;
			checksum |= hex2nib(getDebugChar());

			/* send nack in case of wrong checksum  */
			if (pkt_checksum != checksum) {
				putDebugChar('-');
				continue;
			}

			/* ack */
			putDebugChar('+');

			/* parse already read buffer */
			if (gdb_parse_packet(gdb_ctx->buff))
				continue;

			//if(gdb_ctx->singlestep_enabled || gdb_ctx->breakpoint_enabled)
			//{
				///* this will generate interrupt after one instruction in main code */
				////gdb_enable_Analoginterrupt();
				//ACSR = (1 << ACBG) | (1 << ACIE);
			//}
			//else
			//{
				////gdb_disable_Analoginterrupt();
				//ACSR = 0;
			//}

			/* leave the trap, continue execution */
			return;

		case '-':  /* NACK, repeat previous reply */
			gdb_send_buff(gdb_ctx->buff, gdb_ctx->buff_sz);
			break;
		case '+':  /* ACK, great */
			break;
		//case 0x03:
			///* user interrupt by Ctrl-C, send current state and
			   //continue reading */
			//gdb_send_state(GDB_SIGINT);
			//break;
		default:
			gdb_send_reply(""); /* not supported */
			break;
		}

	}
	
	trap:
	gdb_send_state(GDB_SIGTRAP);
	//gdb_ctx->singlestep_enabled = 0;		/* stepping by single instruction is enabled below for each step */

 }
 }