/*
 * gdb.h
 *
 * Created: 27/03/2017 19:51:15
 *  Author: chris
 */ 


#ifndef GDB_H_
#define GDB_H_

#define __LargeStub__

#include <avr/io.h>
#include <avr/pgmspace.h>
 #include <stdio.h>
 #include <stddef.h>
 #include <string.h>
//#include "serial.h"

//#ifdef __cplusplus
//extern "C" {
	//#endif


/**
 * Maximum number of breakpoints supported.
 * Note that gdb will set temporary breakpoint, for example, for Run to line command
 * in IDE so the actual number of interrupts user can set will be lower.
 */
#define AVR8_MAX_BREAKS       (8)

/** Size of the buffer we use for receiving messages from gdb.
 *  must be in hex, and not fewer than 79 bytes,
    see gdb_read_registers for details */
#define AVR8_MAX_BUFF   	0x50

typedef uint8_t bool_t;
#define FALSE 0
#define TRUE 1



/**
 * Initialize the debugger driver
 * ? Is this our version of set_debug_traps ? (see above)
 * But the interrupts are re-directed in compile time, so we do not need such function.
 */
//void debug_init(struct gdb_context *ctx);
void debug_init(void);

/**
 * Insert breakpoint at the position where this function is called.
 * To set-up breakpoints before compilation.
*/
void breakpoint(void);

/**
 * Send text message to gdb console.
 * The function appends "\n" to the msg.
 * Note that GDB will queue the messages until "\n" (0x0a) is received; then
 * it displays the messages in console.
 */
void debug_message(const char* msg);


/* Reason the program stopped sent to GDB:
 * These are similar to unix signal numbers.
   See signum.h on unix systems for the values. */
#define GDB_SIGINT  2      /* Interrupt (ANSI). */
#define GDB_SIGTRAP 5      /* Trace trap (POSIX). */


/* SRAM_OFFSET is hard-coded in GDB, it is there to map the separate address space of AVR (Harvard)
* to linear space used by GDB. So when GDB wants to read from RAM address 0 it asks our stub for
* address 0x00800000.
* FLASH_OFFSET is always 0
* MEM_SPACE_MASK is used to clear the RAM offset bit. It should not affect the highest possible
* address in flash which is 17-bit for Atmega2560, that is why 0xfffE0000.
*  */
#if defined(__AVR_ATmega1280__) || defined(__AVR_ATmega2560__)

	#define MEM_SPACE_MASK 0x00fe0000
	#define FLASH_OFFSET   0x00000000
	#define SRAM_OFFSET    0x00800000

	#define	UART_ISR_VECTOR	USART0_RX_vect

	/* AVR puts garbage in high bits of return address on stack.
   	   Mask them out */
	// Atmega 1280 PC is 16 bit according to data sheet; Program memory is addressed by words, not bytes.
	// Atmega 2560 PC is 17 bits. We mask the 3rd (HIGH-HIGH) byte of PC in this case, not the 2nd (HIGH) byte.
#if defined(__AVR_ATmega2560__)
	#define RET_ADDR_MASK  0x01
#else
	#define RET_ADDR_MASK  0xff
#endif

#else
	#define MEM_SPACE_MASK 0x00ff0000
	#define FLASH_OFFSET   0x00000000
	#define SRAM_OFFSET    0x00800000	/* GDB works with linear address space; RAM address from GBD will be (real addresss + 0x00800000)*/

	#define	UART_ISR_VECTOR	USART_RX_vect

	/* AVR puts garbage in high bits of return address on stack.
   	   Mask them out
 	 The original version used mask 0x1f, which does not work for code above 16 kB!
	 In 1/2017 changed to 0x3f. The PC is 14 bits on Atmega328 (13 bits on Atmega168), we must
	 out the higher byte, so the mask is actually: 0x3fFF to keep 14 bits.
	 In the code this is used when reading 2 B from RAM to mask the first byte adr+0; adr+1 is not masked
	*/
	#define RET_ADDR_MASK  0x3f
#endif



/* To insert size of the buffer into PacketSize reply*/
#define STR(s) #s
#define STR_VAL(s) STR(s)

#if defined(__AVR_ATmega2560__)	|| defined(__LargeStub__) /* PC is 17-bit on ATmega2560*/
 typedef uint32_t memAddr ;
#else
 typedef uint32_t memAddr;
#endif

   extern volatile uint16_t pc ;
   extern volatile uint16_t targetPC ;
   extern volatile uint16_t frame;

/**
 * Data used by this driver.
 */
struct gdb_context
{

	uint16_t sp;
	memAddr pc;

#if defined(__LargeStub__)
	memAddr breaks [AVR8_MAX_BREAKS];	/* Breakpoints */
	uint8_t breaks_cnt;		/* number of valid breakpoints */
#else
// we will limit to 2 breakpoints to save space and time
	memAddr break0;
	memAddr break1;
#endif

	//uint8_t singlestep_enabled;
	//uint8_t breakpoint_enabled;		/* At least one BP is set */


	uint8_t buff[AVR8_MAX_BUFF+1];
	uint8_t buff_sz;
};


///* Convert number 0-15 to hex */
//#define nib2hex(i) (uint8_t)((i) > 9 ? 'a' - 10 + (i) : '0' + (i))


/*  Prototypes of internal functions */

/* UART routines
 * Names taken from GDB documentation for stub; int replaced by uint8_t */
//static uint8_t rchar(void);		/* Read a single character from the serial port */
//static void putchar(uint8_t c);	/* Write a single character to serial port */
//static void uart_init(void);			/* Our function to initialize UART */
//static void handle_exception(void);	/* Function called when the program stops */
//static inline void gdb_enable_swinterrupt();
//static inline void gdb_disable_swinterrupt();

uint8_t nib2hex(uint8_t i) ;
uint8_t hex2nib(uint8_t hex);
uint8_t parse_hex(const uint8_t *buff, memAddr *hex);
void gdb_send_buff(const uint8_t *buff, uint8_t sz);
void gdb_send_reply(const char *reply);
bool_t gdb_parse_packet(const uint8_t *buff);
void gdb_send_state(uint8_t signo);
void gdb_write_registers(const uint8_t *buff);
void gdb_read_registers(void);
void gdb_write_memory(const uint8_t *buff);
void gdb_read_memory(const uint8_t *buff);
void gdb_insert_remove_breakpoint(const uint8_t *buff);
bool_t gdb_insert_breakpoint(memAddr rom_addr);
void gdb_remove_breakpoint(memAddr rom_addr);

//static inline void restore_regs (void);
//static inline void save_regs1 (void);
//static inline void save_regs2 (void);

//static uint8_t safe_pgm_read_byte(uint32_t rom_addr_b);

uint8_t getDebugChar(void);

/* Write a single character to serial port */
void putDebugChar(uint8_t c);

 
/* Global variables */

/* Our context and pointer to this context.
 * The pointer is used in the original code which receives the struct from main.
 * I keep it so as not to change all the functions even though*/
extern struct gdb_context ctx;
extern struct gdb_context *gdb_ctx;

/* String for PacketSize reply to gdb query.
 * Note: if running out of RAM the reply to qSupported packet can be removed. */
//static char* gdb_str_packetsz = "PacketSize=" STR_VAL(AVR8_MAX_BUFF);

/* PC is 17-bit on ATmega2560; we need 1 more byte for registers but since we will
 * work with the PC as uint32 we need one extra byte; the code then assumes this byte
 * is always zero. */
#if defined(__AVR_ATmega2560__)
	#define GDB_NUMREGBYTES	39			/* Total bytes in registers */
	static unsigned char regs[GDB_NUMREGBYTES];	/* Copy of all registers */
#else
	#define GDB_NUMREGBYTES	37
	// but not storing regs in fixed address, they will be on the stack
#endif


#if defined(__AVR_ATmega2560__)
#define R_PC	*(uint32_t*)(regs+35)	/* Copy of PC register */
#else
#define R_PC	*(uint16_t*)(regs+35)	/* Copy of PC register */
#endif

#define	R_PC_H  *(uint8_t*)(regs+36)	/* High byte of the Copy of the PC */
#if defined(__AVR_ATmega2560__)	/* PC is 17-bit on ATmega2560*/
	#define	R_PC_HH  *(uint8_t*)(regs+37)	/* High-High byte of the Copy of the PC */
	#define R_SREG	*(uint8_t*)(regs+32)	/* Copy of SREG register */
#endif

#ifdef __LargeStub__
extern unsigned char regs[GDB_NUMREGBYTES];	/* Copy of all registers */
#define R_SP	*(uint16_t*)(regs+33)	/* Copy of SP register */
#define R_SREG	*(uint8_t*)(regs+32)	/* Copy of SREG register */
#endif

#if FLASHEND > 0x1FFFF
#       define ASM_GOTO "jmp "
#elif FLASHEND > 0x2000
#       define ASM_GOTO "jmp "
#else
#       define ASM_GOTO "rjmp "
#endif


/* Helper used only for internal testing of the stub */
#if 0
uint8_t test_check_stack_usage(void);
#endif


void gdb_stepping(void);
//needs to be extern "c" so the assembler can find it (C++ mangles function names)
//extern "C"
//{
//void gdb_break(void);
//}
void save_regs1(void);
void save_regs2(void);
void restore_regs(void);

// serial connection
void uart_init(void);
void delay_us(uint16_t delay);
uint8_t getDebugChar(void);
void putDebugChar(uint8_t c);
uint8_t checkDebugChar(void);

//#ifdef __cplusplus
//}
//#endif




#endif /* GDB_H_ */