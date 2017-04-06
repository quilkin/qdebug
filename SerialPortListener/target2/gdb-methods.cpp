/*
 * gdb_methods.c
 *
 * Created: 27/03/2017 20:35:17
 *  Author: chris
 */ 
 #include "gdb.h"

 

 /* ---------- Helpers ------------- */

 /* Convert a hexadecimal digit to a 4 bit nibble. */
 //__attribute__((optimize("-Os")))
 uint8_t hex2nib(uint8_t hex)
 {
	 if (hex >= 'A' && hex <= 'F')
	 return 10 + (hex - 'A');
	 else if (hex >= 'a' && hex <= 'f')
	 return 10 + (hex - 'a');
	 else if (hex >= '0' && hex <= '9')
	 return hex - '0';

	 return 0xff;
 }

 /* Convert number 0-15 to hex */
 uint8_t nib2hex(uint8_t i)
 {
	 return ((i) > 9 ? 'a' - 10 + (i) : '0' + (i));
 }


 __attribute__((optimize("-Os")))
 uint8_t parse_hex(const uint8_t *buff, memAddr *hex)
 {
	 uint8_t nib, len;
	 for (*hex = 0, len = 0; (nib = hex2nib(buff[len])) != 0xff; ++len)
	 *hex = (*hex << 4) + nib;
	 return len;
 }

 
/* This is the main "dispatcher" of commands from GDB
 * If returns false, the debugged program continues execution */
__attribute__((optimize("-O0")))
bool_t gdb_parse_packet(const uint8_t *buff)
{
	switch (*buff) {
	case '?':               /* last signal */
		gdb_send_reply("S05"); /* signal # 5 is SIGTRAP */
		break;
	case 'g':               /* read registers */
		gdb_read_registers();
		break;

	case 'm':               /* read memory */
		gdb_read_memory(buff + 1);
		break;

// small debug stub won't be able to write to memory or registers
#if defined(__LargeStub__)
	case 'G':               /* write registers */
		gdb_write_registers(buff + 1);
		break;
	case 'M':               /* write memory */
		gdb_write_memory(buff + 1);
		break;
#else
	case 'G':               /* write registers */
		gdb_send_reply("");  /* not supported */
	break;
	case 'M':               /* write memory */
		gdb_send_reply("");  /* not supported */
	break;
#endif
	case 'D':               /* detach the debugger */
	case 'k':               /* kill request */
		gdb_send_reply("OK");
		return FALSE;

	case 'c':               /* continue */
		return FALSE;
	case 'C':               /* continue with signal */
	case 'S':               /* step with signal */

		gdb_send_reply(""); /* not supported */
		break;
	case 's':               /* step */

		//gdb_ctx->singlestep_enabled = 1;
		return FALSE;

	case 'z':               /* remove break/watch point */
	case 'Z':               /* insert break/watch point */
		gdb_insert_remove_breakpoint(gdb_ctx->buff);
		break;

	default:
		gdb_send_reply("");  /* not supported */
		break;
	}

	return TRUE;
}

__attribute__((optimize("-Os")))
void gdb_insert_remove_breakpoint(const uint8_t *buff)
{
	memAddr rom_addr_b, sz;
	uint8_t len;

	/* skip 'z0,' */
	len = parse_hex(buff + 3, &rom_addr_b);
	/* skip 'z0,xxx,' */
	parse_hex(buff + 3 + len + 1, &sz);

	/* get break type */
	switch (buff[1]) {
	case '0': /* software breakpoint */
		if (buff[0] == 'Z')
			gdb_insert_breakpoint(rom_addr_b >> 1);
		else
			gdb_remove_breakpoint(rom_addr_b >> 1);

		gdb_send_reply("OK");
		break;

	default:
		/* we do not support other breakpoints, only software */
		gdb_send_reply("");
		break;
	}
}

__attribute__((optimize("-Os")))
bool_t gdb_insert_breakpoint(memAddr rom_addr)
{
#if defined(__LargeStub__)
	uint8_t i;
	memAddr* p = gdb_ctx->breaks;
	/* First look if the breakpoint already exists */

	for (i = gdb_ctx->breaks_cnt; i > 0; i--)
	{
		if (*p++ == rom_addr)
			return TRUE;
	}

	if ( gdb_ctx->breaks_cnt >= AVR8_MAX_BREAKS)
		return FALSE;	/* no more breakpoints available */

	gdb_ctx->breaks[gdb_ctx->breaks_cnt++] = rom_addr;
#else
// limited to 2 breakpoints
	if (gdb_ctx->break0 == 0) {
		gdb_ctx->break0 = rom_addr;
	}
	else
	if (gdb_ctx->break1 == 0) {
		gdb_ctx->break1 = rom_addr;
	}
	else 
	{
	/* no more breakpoints available */
		return FALSE;
	}

#endif
	//gdb_ctx->breakpoint_enabled = 1;	/* at least one breakpoint exists */
	return TRUE;

	/* Note: GDB will always delete all breakpoints (BP) when the program stops
	 * on a BP or after step. It will then set them again before continue (c)
	 * or step.
	 * */
}

void gdb_remove_breakpoint(memAddr rom_addr)
{
#if defined(__LargeStub__)
	uint8_t i, j;

	for (i = 0, j = 0; j < gdb_ctx->breaks_cnt; i++, j++)
	{
		/* i is target, j is source index */
		if ( gdb_ctx->breaks[i] == rom_addr )
		{
			/* this is the BP to remove */
			j++;
			if ( j >= gdb_ctx->breaks_cnt )
			{
				gdb_ctx->breaks[i] = 0;
				break;		/* removing the last BP in the array */
			}
		}

		/* normally, this will do nothing but after the breakpoint-to-be-removed is found and
		 * j is incremented, it will shift the remaining breakpoints. */
		gdb_ctx->breaks[i] = gdb_ctx->breaks[j];

	}	// end for

	if ( j > i )	/* if we found the BP to be removed, there is now one less */
		gdb_ctx->breaks_cnt--;

	//if ( gdb_ctx->breaks_cnt == 0 )
		//gdb_ctx->breakpoint_enabled = 0;	/* if there are no breakpoints */

#else
		// limited to 2 breakpoints
		if (gdb_ctx->break0 == rom_addr) {
			gdb_ctx->break0 = 0;
		}
		else
		if (gdb_ctx->break1 == rom_addr) {
			gdb_ctx->break1 = 0;
		}
		else
		{
			///* no more breakpoints to remove */
			//gdb_ctx->breakpoint_enabled = 0;
		}
#endif

}

#if defined(__LargeStub__)


__attribute__((optimize("-Os")))
void gdb_write_memory(const uint8_t *buff)
{
	memAddr addr, sz;
	uint8_t i;

	buff += parse_hex(buff, &addr);
	/* skip 'xxx,' */
	buff += parse_hex(buff + 1, &sz);
	/* skip , and : delimiters */
	buff += 2;

	if ((addr & MEM_SPACE_MASK) == SRAM_OFFSET) {
		addr &= ~MEM_SPACE_MASK;
		uint8_t *ptr = (uint8_t*)(uintptr_t)addr;
		for ( i = 0; i < sz; ++i) {
			ptr[i]  = hex2nib(*buff++) << 4;
			ptr[i] |= hex2nib(*buff++);
		}
	}
	//else if ((addr & MEM_SPACE_MASK) == FLASH_OFFSET){
	///* jd: for now not implemented */
	///* posix EIO error */
	//gdb_send_reply("E05");
	//return;
	//
	//}
	else {
		/* posix EIO error */
		gdb_send_reply("E05");
		return;
	}
	gdb_send_reply("OK");
}

#else  // small stub

#endif  // small stub


__attribute__((optimize("-Os")))
void gdb_read_memory(const uint8_t *buff)
{
	memAddr addr, sz;
	uint8_t i;

	buff += parse_hex(buff, &addr);
	/* skip 'xxx,' */
	parse_hex(buff + 1, &sz);

	//if ((addr & MEM_SPACE_MASK) == SRAM_OFFSET) {
	if (1) {
		addr &= ~MEM_SPACE_MASK;
		uint8_t *ptr = (uint8_t*)(uintptr_t)addr;
		for (i = 0; i < sz; ++i) {
			uint8_t b = ptr[i];
			/* XXX: this is ugly kludge, but what can I do?
					AVR puts return address on stack with garbage in high
					bits (they say you should mask out them, see Stack Pointer
					section at every AVR datasheet), but how can I understand
					that this word is ret address? To have valid backtrace in
					gdb, I'am required to mask every word, which address belongs
					to stack. */
#if defined(__AVR_ATmega2560__)
			/* TODO: for ATmega2560 the 3rd byte of PC should be masked out, but
			 * how do we know when the 3rd byte is read?
			 * The code for other derivatives can mask every word, but for 2560 we need to mask
			 * only one byte of one of the two words that GDB reads...or does GDB read 3 bytes?
			 * If yes, the code should be: */
			 if (i == 0 && sz == 3 && addr >= gdb_ctx->sp)
				b &= RET_ADDR_MASK;
#else
			if (i == 0 && sz == 2 && addr >= gdb_ctx->sp)
				b &= RET_ADDR_MASK;
#endif
			gdb_ctx->buff[i*2 + 0] = nib2hex(b >> 4);
			gdb_ctx->buff[i*2 + 1] = nib2hex(b & 0xf);
		}
	}
	//cjf: not reading flash memory

	//else if ((addr & MEM_SPACE_MASK) == FLASH_OFFSET){
		//addr &= ~MEM_SPACE_MASK;
		//for (i = 0; i < sz; ++i) {
			//uint8_t byte = safe_pgm_read_byte(addr + i);
			//gdb_ctx->buff[i*2 + 0] = nib2hex(byte >> 4);
			//gdb_ctx->buff[i*2 + 1] = nib2hex(byte & 0xf);
		//}
	//}
	else {
		/* posix EIO error */
		gdb_send_reply("E05");
		return;
	}
	gdb_ctx->buff_sz = sz * 2;
	//gdb_send_buff(gdb_ctx->buff, 0, gdb_ctx->buff_sz, FALSE);
	gdb_send_buff(gdb_ctx->buff, gdb_ctx->buff_sz);
}

/* ---------- GDB RCP packet processing  ------------- */


__attribute__((optimize("-Os")))
void gdb_send_buff(const uint8_t *buff, uint8_t sz)
{
	uint8_t sum = 0;

	putDebugChar('$');
	while ( sz-- > 0)
	{
		putDebugChar(*buff);
		sum += *buff;
		buff++;
	}
	putDebugChar('#');
	putDebugChar(nib2hex((sum >> 4) & 0xf));
	putDebugChar(nib2hex(sum & 0xf));
}


__attribute__((optimize("-Os")))
void gdb_send_reply(const char *reply)
{
	gdb_ctx->buff_sz = strlen(reply);
	if ( gdb_ctx->buff_sz > (AVR8_MAX_BUFF - 4))
		gdb_ctx->buff_sz = AVR8_MAX_BUFF - 4;

	memcpy(gdb_ctx->buff, reply, gdb_ctx->buff_sz);
	gdb_send_buff(gdb_ctx->buff, gdb_ctx->buff_sz);

}


//
//static uint8_t safe_pgm_read_byte(uint32_t rom_addr_b)
//{
	//#ifdef pgm_read_byte_far
	//if (rom_addr_b >= (1l<<16))
	//return pgm_read_byte_far(rom_addr_b);
	//else
	//#endif
	//return pgm_read_byte(rom_addr_b);
//}
