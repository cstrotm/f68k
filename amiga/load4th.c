/***********************************************************
 *
 *   LOAD4TH.c
 *
 ************************************************************
 *	AMIGA-Version of the Loader program for a F68K image file
 *
 *
 *	This loader tries to open a file F68K.CFG which
 *	holds information about the F68K system to be loaded.
 ************************************************************
 * original by Jörg Plewe
 *
 * ported to Amiga (Aztec C5.0d) by Wolfgang Schemmert, 24.10.91
 * ported to VBCC by Carsten Strotmann. 19.01.2021
 *
 * Can be started from CLI as well as from Workbench
 * When started from workbench, file "Load4th.info" has to be
 * in the same directory.
 *
 * error found, but still not repaired:
 * The Amiga DOS console only prints 77 chars per line,
 * the rest of the line is wrapped. Especially, in the
 * fullscreen editor up to three characters are overwritten,
 * if the line above is more than 77 chars long.
 ***********************************************************/


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <proto/dos.h>
#include <clib/dos_protos.h>
#include <clib/exec_protos.h>
#include <proto/exec.h>
#include <intuition/intuition.h>

#define CODESIZE 0x20000L
#define DATASIZE 0x20000L
#define TIBSIZE  2048
#define MAX_DEVICES 10
#define BPB  2048                 /* Bytes Per Block */

long key_quest(void);
long key(void);
void emit(long);
void setcursor(void);
void getcursor(void);
long r_w(void*,long,long);
long writesys(unsigned long*,unsigned long);
long readsys(unsigned long*,unsigned long);

long read_paras(long*);
long read_segments(void**,void**);

struct IntuitionBase *IntuitionBase;
char devicename[MAX_DEVICES][FILENAME_MAX];
FILE* device[MAX_DEVICES];
FILE* infile;
long roottable[MAX_DEVICES*2 +1];
BPTR handle; /* specific to Amiga-DOS: exactly adjusts to .L boudary */
unsigned char readbuf[2]={0,0};
unsigned char writebuf[82];
char escflag=0,options=0;
char edrow,edcol,oldrow=0,oldcol=0;

long codesz = CODESIZE;
long datasz = DATASIZE;
char imagename[FILENAME_MAX] = "F68K.IMG";
char outfilename[FILENAME_MAX] = "F68K.OUT";
char cfgname[FILENAME_MAX] = "F68K.CFG";
long oldstack = 0L;     /*to remember UserStack*/
long errcnt = 0L;

void *codeseg,*dataseg;
void *paradr;

FILE *dumpfile;
char dumpname[FILENAME_MAX] = "dump.dat";


int main(int argc, const char *argv[])
/***********************************************************
 * ported to Amiga by Wolfgang Schemmert, 24.20.91
 *
 * Rev 9.11.91: return stack cleanup OK now
 ***********************************************************/

{

        long keytable[]		= {1,0};
        long keyqtable[]	= {1,0};
        long emittable[]	= {1,0};
        long r_wtable[]		= {1,0};
        long readsystable[]	= {1,0};
        long writesystable[]= {1,0};
        void (*f68k)();

        struct forthparas
        {
                long registers[16];	/* to be filled by F68K */
                void *data;
                void *code;
                void *datastack;
                void *retstack;
                void *TIBptr;
                long codelen;
                long datalen;
                void *emittable;
                void *keytable;
                void *keyqtable;
                void *r_wtable;
                void *readsystable;
                void *writesystable;
                void *roottable;
        } forthparas;

        typedef void FUNC(struct forthparas*);


        keytable[1]     = (long) key;
        keyqtable[1]    = (long) key_quest;
        emittable[1]    = (long) emit;
        r_wtable[1]     = (long) r_w;
        readsystable[1] = (long) readsys;
        writesystable[1]= (long) writesys;

        forthparas.emittable 	= emittable;
        forthparas.keytable 	= keytable;
        forthparas.keyqtable 	= keyqtable;
        forthparas.r_wtable 	= r_wtable;
        forthparas.readsystable = readsystable;
        forthparas.writesystable= writesystable;
        forthparas.roottable 	= roottable;


        if(argc==2)
                switch (*argv[1])
                {
                case 's': 	/* Dump-File spez.f. Screens anlegen */
                case 'S':
                        if (*(argv[1]+1)==0)
                                options = 'S';
                        break;
                case 'p': 	/* Dump-File f.allg. Protokollierung */
                case 'P':
                        if (*(argv[1]+1)==0)
                                options = 'P';
                        break;
                default:
                        strcpy(cfgname,argv[1]);
                }

/*	IntuitionBase = (struct IntuitionBase *)
    OpenLibrary("intuition.library",0L);
    if (IntuitionBase == NULL) exit (FALSE); */

        handle= Open("RAW:0/0/640/256/ F68K_on_AMIGA ",MODE_OLDFILE);


        if (options)
        {
                dumpfile = (fopen(dumpname,"wb")) ;
                if (!dumpfile)	Write(handle,"Can't open dumpfile\n", 19);
        }


        if ( ! read_paras(roottable)) goto closeRAW;
        forthparas.codelen	= codesz;
        forthparas.datalen	= datasz;

        if ( ! read_segments(&codeseg,&dataseg)) goto closeRAW;
        forthparas.code		= codeseg;
        forthparas.data		= dataseg;
        forthparas.datastack= (void*)((long)dataseg+datasz-TIBSIZE);
        forthparas.retstack	= (void*)((long)dataseg+datasz);
        forthparas.TIBptr	= (void*)((long)dataseg+datasz-TIBSIZE);

        if (errcnt)
                emit(10L);  /* 1x CR senden */

/* 		oldstack = SuperState(); could switch into SuperVisor State*/

        paradr = &forthparas;
        f68k=(void*)codeseg;
        (*f68k)(paradr);
/*
  #asm
  movem.l a0-a6,-(a7)
  movea.l _codeseg,a0
  move.l _paradr,-(a7)
  jsr (a0)
  addq.l #4,a7
  movem.l (a7)+,a0-a6
  #endasm
*/

/*		if (oldstack)
			UserState(oldstack);*/

        errcnt = 0L;

closeRAW:
        if (errcnt)
        {
                Write(handle,"EXIT: press RETURN",19);
                key();
        }

        Close(handle);

/*		CloseLibrary(IntuitionBase);	*/

        if(options) fclose(dumpfile);

}


/***********************************************************
 *                                                          *
 *       the F68K I/O-functions                             *
 *                                                          *
 ***********************************************************/


long key_quest(void)
/**********************************************************
 * returns TRUE, if key input available, else FALSE
 * created by Wolfgang Schemmert 24.10.91
 **********************************************************/

{

        return (WaitForChar(handle,1000));

}

long key(void)
/**********************************************************
 * created by Wolfgang Schemmert 24.10.91
 * Rev 10.11.91: conversion of Amiga-CSI-Seq to ^-ASCII
 **********************************************************/

{
        Read(handle,readbuf,1);

        if (readbuf[0] == 0x7f)   	/* $7f=Delete-Taste */
                return 7L;     /* entspr. ^G-Taste */

        if (readbuf[0] == 6)	   	/* 6 =^F-Taste */
                return 31L;     /* entspr. ^?-Taste */

        if (readbuf[0] == 22)  		/* 22 =^V-Taste */
                return 28L;     /* entspr. ^<-Taste */

        if (readbuf[0] == 28) 	   	/* 28 =^\-Taste */
                return 30L;     /* entspr. ^>-Taste */

        if (readbuf[0] < 0x9b)    	/* $9b = CSI */
                return (long) readbuf[0];

        if (readbuf[0] == 0x9b)
        {
                Read(handle,readbuf,1);

/* this code translation into ^ASCII is undertaken to be in
   a maximum of compatibility with the original implementation
   of the F68K. Perhaps unnecessary, but I'm too lazy to test
   it completely out */

                switch (readbuf[0])
                {
                case 65:           /* ^E */
                        return 5L;
                case 66:           /* ^X */
                        return 24L;
                case 67:           /* ^D */
                        return 4L;
                case 68:           /* ^S */
                        return 19L;
                default: return (long) readbuf[0];
                }
        }
}

void emit(long ch)
/**********************************************************
 * it's very slow due to the "great"
 * and highly sopisticated Amiga-Exec
 * speedup would require direct access to the rastport
 * or full-line emitting

 * VT52 codes are bad fitting with the Amiga's internal
 * cursor commands. So, a lot of patchwork is necessary

 * The codes to be send to the Amiga are listed in HEX
 * notation to be in compatibility with most literature
 * on Amiga
 ***********************************************************
 * created by Wolfgang Schemmert 24.10.91
 * Rev 11.11.91: conversions of f68k-outputs to Amiga-CSI-Seq.
 *               just at the beginning of the carneval
 * Rev 16.11.91: some additional esc features
 *               P and S option
 * Rev 18.11.91: esc d and esc o, getcursor() implemented
 **********************************************************/

{
        char i=0,k;

/* in the P-mode all into the file, in the S-mode kick out CR */
        if ((options=='P')||((options=='S') && (ch!=13L)))
                putc((int) ch,dumpfile);

/* in the S-mode always swallow the character after LF */
        if (options =='W') options = 'S';
        if ((options=='S')&& (ch==10L)) options = 'W';


        writebuf[0]= (unsigned char) ch;
        switch (escflag)
        {
        case 0:			/* normal single character output */
                switch (writebuf[0])
                {
                case 27:					/* ESC */
                        escflag = 1;
                        return;
                case 8:						/* Backspace */
                case 9:						/* TAB */
                case 10:	                /* Linefeed */
                case 13:					/* Carr.Ret */
                        Write(handle,writebuf,1);
                        return;

                case 7: 	/* Bell = visual */
                        /* DisplayBeep(0L); */
                        return;

/* To be in a maximum of compatibility with the original
   implementation of F68K, the cursor control sequences from
   the Amiga keyboard were translated into ^ASCII-notation.
   In the non-editor-mode they are looped through by F68K */

                case 4: 					/* ^D */
                        writebuf[0]=0x9b;       /* CSI = $9b */
                        writebuf[1]=67;
                        Write(handle,writebuf,2);
                        return;
                case 5:						/* ^E */
                        writebuf[0]=0x9b;
                        writebuf[1]=65;
                        Write(handle,writebuf,2);
                        return;
                case 19:					/* ^S */
                        writebuf[0]=0x9b;
                        writebuf[1]=68;
                        Write(handle,writebuf,2);
                        return;
                case 24:					/* ^X */
                        writebuf[0]=0x9b;
                        writebuf[1]=66;
                        Write(handle,writebuf,2);
                        return;
                default: ;
                }
                if (writebuf[0] < 32)
                        return;    /* filter out any other control codes */

                Write(handle,writebuf,1);	/* normal Character output */
                return;

/* Cursor control of the F68K Fullscreen-Editor is using VT52-Codes,
   which are translated into Amiga-comprehensible notation below */

        case 1: /* ESC was emit'd before and Seq. not yet worked off */
                escflag = 0;
                switch (writebuf[0])
                {
                case 'A': 					/* Cursor Up */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x41;
                        Write(handle,writebuf,2);
                        return;
                case 'B':					/* Cursor down */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x42;
                        Write(handle,writebuf,2);
                        return;
                case 'C':					/* Cursor right */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x43;
                        Write(handle,writebuf,2);
                        return;
                case 'D':					/* Cursor left */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x44;
                        Write(handle,writebuf,2);
                        return;
                case 'E':					/* CLS */
                        writebuf[0]=0x0c;
                        Write(handle,writebuf,1);
                        return;
                case 'I':					/* insert line above */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x4c;
                        Write(handle,writebuf,2);
                        return;
                case 'H':					/* Cursor home */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x48;
                        Write(handle,writebuf,2);
                        return;
                case 'J':        			/* delete end of page */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x4a;
                        Write(handle,writebuf,2);
                        return;
                case 'K':					/* delete end of line */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x4b;
                        Write(handle,writebuf,2);
                        return;
                case 'L':					/* insert line below */
                        writebuf[0]=10;
                        writebuf[1]=0x9b;
                        writebuf[2]=0x4c;
                        Write(handle,writebuf,3);
                        return;
                case 'M': 					/* delete line */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x4d;
                        Write(handle,writebuf,2);
                        return;

/* Free cursor positioning is requested by the code ESC Y a b
   -where a is the vertical position plus 32 (start count at 0)
   and b is the horizontal position plus 32
   so, we have to switch decode runs for two additional characters */

                case 'Y':
                        escflag = 2;
                        return;

                case 'e':				/* Cursor ON */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x20;
                        writebuf[2]=0x70;
                        Write(handle,writebuf,3);
                        return;

                case 'f':				/* Cursor OFF */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x30;
                        writebuf[2]=0x20;
                        writebuf[3]=0x70;
                        Write(handle,writebuf,4);
                        return;

                case 'j':   	/* save actual cursor position */
                        oldrow = edrow;
                        oldcol = edcol;
                        return;

                case 'k':   	/* restore cursor position */
                        edrow = oldrow;
                        edcol = oldcol;
                        setcursor();
                        return;

                case 'l':  		/* erase line */
                        writebuf[0]=0x9b;
                        writebuf[1]=0x4d; 	/* erase */
                        writebuf[2]=0x9b;
                        writebuf[3]=0x4c;	/* and insert new */
                        Write(handle,writebuf,4);
                        return;


                case 'p': 		/* switch to reverse video */
                        writebuf[0]=0x9b;
                        strcpy((char*)(writebuf+1),"7;31;40m");
                        Write(handle,writebuf,9);
                        return;

                case 'q': 		/* switch to normal video */
                        writebuf[0]=0x9b;
                        strcpy((char*)(writebuf+1),"0;31;40m");
                        Write(handle,writebuf,9);
                        return;

                case 'd':		/* delete start of page */
                        i = -1;
                case 'o':		/* delete start of line */

                        getcursor();

                        writebuf[0] = 0x0d;
                        k = 1;
                        while (edcol>1)
                        {
                                edcol --;
                                writebuf[k]= ' ';
                                k ++;
                        }
                        writebuf[k] = 0x0d;
                        Write(handle,writebuf,k+1);

                        if (!i) return; /* line only */

                        while (edrow>1)
                        {
                                edrow --;
                                writebuf[0]=0x9b;
                                writebuf[1]=0x46; /*curs. 1 row up->col 1*/
                                writebuf[2]=0x9b;
                                writebuf[3]=0x4b; /* del_eol */
                                Write(handle,writebuf,4);
                        }
                        return;

                case 'v':		/* switch the wrap mode */
                case 'w':		/* switch to nowrap mode */
                        return;		/* these opt's not implemented */

                default:
                        Write(handle,writebuf,1);
                        return;
                }

        case 2:	  	/* ESC Y... 1st parameter = new vertical pos.*/

                escflag = 3;
                edrow = writebuf[0] -31;
                return;


        case 3:	  	/* ESC Y... 2nd parameter = new horizontal pos.*/

                escflag = 0;
                edcol = writebuf[0] -31;
                setcursor();
                return;
        }
}


void setcursor(void)
/****************************************************************
 * sets cursor to position (edcol/edrow)
 * created by Wolfgang Schemmert 16.11.91
 ****************************************************************/

{
        char ones,tens;

        writebuf[0]=0x9b;
        ones =edrow;
        tens = 48;
        while (ones>9)
        {
                ones -= 10;
                tens ++;
        }
        writebuf[1] = tens;
        writebuf[2] = ones + 48;

        writebuf[3] = 0x3b;
        ones = edcol;
        tens = 48;
        while (ones>9)
        {
                ones -=10;
                tens ++;
        }
        writebuf[4] = tens;
        writebuf[5] = ones + 48;

        writebuf[6] = 0x48;
        Write(handle,writebuf,7);

}


void getcursor(void)
/****************************************************************
 * gets position of cursor (edcol/edrow)
 * created by Wolfgang Schemmert 18.11.91
 ****************************************************************/

{

        writebuf[0] = 0x9b;
        writebuf[1] = 0x36;
        writebuf[2] = 0x6e;
        Write(handle,writebuf,3); 	/* order cursor position

returns in the format: 9b <line> 3b <column> 52  where
<line> and <column> are decimal in 1 or 2 ASCII ciphers */

        Read(handle,writebuf,3);
        edrow = writebuf[1]-48;

        if (writebuf[2] != 0x3b)
        {
                edrow = edrow*10 + writebuf[2]-48;
                Read(handle,writebuf,1);
        }

        Read(handle,writebuf,2);
        edcol = writebuf[0]-48;
        if (writebuf[1] != 0x52) edcol = edcol*10 + writebuf[1]-48;

}


long r_w(void *buffer,long block,long flag)
/**********************************************************
 * ported to Amiga by Wolfgang Schemmert, 24.10.91
 **********************************************************/

{
        int i, dev;
        long rootblk=0L, maxblock=0L;
        long bloffs;


        for(i=0; i<roottable[0]; i++)  /* find device */
                if( (roottable[2*i+1] >= rootblk) && (block >= roottable[2*i+1]))
                {
                        maxblock += roottable[2*i+2];
                        rootblk = roottable[2*i+1];
                        dev = i;
                }

        if(block >= maxblock)   /* block in range? */
        {
                goto bad;
        }

/*
	if(Mediach(0) != 0)
  {
  rewind(device[dev]);
  fclose(device[dev]);
  device[dev]=fopen(devicename[dev],"rb+");
  }
*/
        bloffs = (block-rootblk)*BPB;
        if(fseek(device[dev],bloffs,SEEK_SET))
                goto bad;   /* fseek() gibt 0 bei OK */

        if (ftell(device[dev]) != bloffs)
                goto bad;

        if(flag!=0L)
        {
                if (fwrite(buffer,1,BPB,device[dev]) != BPB)
                        goto bad;
        }
        else
        {
                if (fread(buffer,1,BPB,device[dev]) != BPB)
                        goto bad;
        }

        return TRUE;


bad:

        return FALSE;
}



long readsys(unsigned long *buffer,unsigned long count)
/**********************************************************
 * taken from the orininal without modification
 **********************************************************/

{

        if ( fread(buffer,count,1,infile) != 1)
        {
                return FALSE;
        }

        return TRUE;
}


long writesys(unsigned long *buffer,unsigned long count)
/**********************************************************
 * taken from the original without modification
 **********************************************************/

{
        static FILE *out = NULL;

        if(!out)
                if( (out = fopen(outfilename,"wb"))== NULL)
               	{
                        return FALSE;
               	}

        if ( fwrite(buffer,count,1,out) != 1)
        {
                return FALSE;
        }

        return TRUE;
}

/***********************************************************
 *       end of I/O functions                               *
 ***********************************************************/



long read_paras(long *roottable)
/***********************************************************
 * ported to Amiga by Wolfgang Schemmert, 24.10.91
 ***********************************************************/

{
        FILE *paras;
        int devices, dev;
        long devicesize[MAX_DEVICES];
        char infilename[FILENAME_MAX];
        int i;
        long startblock = 0;


        if( (paras=fopen(cfgname,"r"))==NULL)
        {
                Write(handle,"*** F68K loader warning: configuration file F68K.CFG not found\n",62);
                errcnt++;
                return TRUE;
        }
        if( !fscanf(paras,"image: %s\n",imagename))
				{
                Write(handle,"*** F68K loader warning: no imagefile given in F68K.CFG, suppose F68K.IMG\n",74);
                errcnt++;
				}
        if( !fscanf(paras,"code: 0x%lx\n",&codesz))
				{
                Write(handle,"*** F68K loader warning: no codesize given in F68K.CFG, default taken\n",70);
                errcnt++;
				}
        if( !fscanf(paras,"data: 0x%lx\n",&datasz))
				{
                Write(handle,"*** F68K loader warning: no datasize given in F68K.CFG, default taken\n",70);
                errcnt++;
				}
        if( !fscanf(paras,"input: %s\n", infilename))
        {
                Write(handle,"*** F68K loader warning: no input file given in F68K.CFG, suppose F68K.IN\n",73);
                errcnt++;
                strcpy(infilename,"F68K.IN");
        }
        if( (infile = fopen(infilename,"rb"))==NULL)
				{
                Write(handle,"*** F68K loader warning: cannot open input file, READSYS not available\n",71);
                errcnt++;
				}
        if( !fscanf(paras,"output: %s\n", outfilename))
				{
                Write(handle,"*** F68K loader warning: no output file given in F68K.CFG, suppose F68K.OUT\n",77);
                errcnt++;
				}
        if( !fscanf(paras,"devices: %d\n",&devices))
				{
                Write(handle,"*** F68K loader warning: no number of devices given in F68K.CFG\n",64);
                errcnt++;
				}
        if( devices == 0)
				{
                Write(handle,"*** F68K loader warning: no block storage device available\n",59);
                errcnt++;
				}
        if( devices > MAX_DEVICES )
        {
                Write(handle,"*** F68K loader error: too much devices (max. 10 devices available)\n",67);
                errcnt++;
                return FALSE;
        }
        for(i=0; i<devices; i++)
        {
                if( fscanf(paras,"d%d: ",&dev) && (dev>=0) && (dev<MAX_DEVICES))
                        fscanf(paras,"%s\n",devicename[dev]);
                else
                {
                        Write(handle,"*** F68K loader error: invalid device specification\n",52);
                        errcnt++;
                        return FALSE;
                }
                if( (device[dev]=fopen(devicename[dev],"rb+")) != NULL)
                {
                        fseek(device[dev],0L,SEEK_END);
                        devicesize[dev] = (long)ftell(device[dev]);
                        fseek(device[dev],0L,SEEK_SET);
                        roottable[2*dev+1] = startblock;
                        roottable[2*(dev+1)] = devicesize[dev]/BPB;
                        startblock += devicesize[dev]/BPB;
                }
                else
                {
                        Write(handle,"*** F68K loader warning: device cannot be accessed\n",51);
                        errcnt++;
                        roottable[2*dev+1] = -1;
                        roottable[2*(dev+1)] = 0;
                }
        }
        roottable[0] = devices;

        return TRUE;
}


long read_segments(void **codeseg,void **dataseg)
/**********************************************************
 * ported to Amiga by Wolfgang Schemmert, 24.10.91
 **********************************************************/

{
        FILE *image;
        size_t actual;

        struct header
        {
                char magic[2];
                unsigned long codesize;
                unsigned long datasize;
                short int dont_care[9];
        } header;


        if( ((*codeseg = malloc(codesz)) == NULL) |
            ((*dataseg = malloc(datasz)) == NULL)) {
                Write(handle,"*** F68K loader error:  segments allocation fault\n",49);
                errcnt++;
                return FALSE;
        }

        if( (image=fopen(imagename,"rb")) == NULL ) {
                Write(handle,"*** F68K loader error:  image file not found\n",45);
                errcnt++;
                return FALSE;
        }

        if ( fread(&header,(size_t)sizeof(header), 1, image) < 1 ) {
                Write(handle,"*** F68K loader error:  image file read error (header)\n",55);
                errcnt++;
                return FALSE;
        }

        if(header.magic[0] != 'J' || header.magic[1] != 'P') {
                Write(handle,"*** F68K loader error:  this is not an F68K image\n",50);
                errcnt++;
                return FALSE;
        }

        if( fread(*codeseg, header.codesize, 1, image) < 1) {
                Write(handle,"*** F68K loader error:  image file read error (code)\n",53);
                errcnt++;
                return FALSE;
        }

        if( fread(*dataseg, header.datasize, 1, image) < 1) {
                Write(handle,"*** F68K loader error:  image file read error (data)\n",53);
                errcnt++;
                return FALSE;
        }
        return TRUE;
}
