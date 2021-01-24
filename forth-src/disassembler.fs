\ (1) Disassembler 680XX, (C) R. Scharnagl, not completed        JPS 04-18-92

    variable &at variable &ln           \ Beginn und Laenge zu disassembl. Code
    variable &lc                        \ Laengen-Zeichen falls variierbar
    variable &fl                        \ Flags zum Befehl, auch informativ

: &at! ( addr -- ) -2 and &at ! ;       \ speichere Adresse even
: &at@ ( -- addr ) &at @ ;              \ hole Adresse Wort 1
: &atx ( -- addr ) &at@ &ln @ + ;       \ hole Adresse Wort x
: &atx@+ ( -- w ) &atx w@ 2 &ln +! ;    \ hole Wort x und zaehle Bytes

: case? ( l1 l2 -- [l1] flag ) over = dup IF nip THEN ; \ Hilfs-Konstrukt

: &pl ( -- c ) pad c@ ;                             \ ermittle Puffer-Laenge
: &p+ ( c -- ) &pl + pad c! ;                       \ erhoehe Puffer-Counter
: &px ( -- addr ) pad count + ;                     \ hinter Puffer-Ende

: &stfl ( w -- ) &fl @ or &fl ! ;                   \ setze Flags zusaetzlich
: &tflg ( w -- flag) &fl @ and ;                    \ teste Flag auf gesetzt
: &stwa ( w m -- w ) over and IF $2000 &stfl THEN ; \ setze ggf. Warn-Flag
: &sterr ( -- ) drop $8000 &stfl ;                  \ setze Error-Flag
-->



\ (2) Disassembler                                               JPS 04-18-92

: &_c ( c -- ) &px c! 1 &p+ ;                   \ Zeichen an Puffer haengen
: &_?c ( c -- ) bl case? IF ELSE &_c THEN ;     \ (1) Nichtblank anhaengen
: &_.c ( c -- ) [char] . &_c &_c ;              \ Laengensuffix anhaengen
: &_.x ( c -- ) dup &lc ! &_.c ;                \ Laengensuff. anh. u. sp.

: &_.WL ( flag -- )                             \ .L oder .W anhaengen
  IF [ char L ] literal ELSE [ char W ] literal THEN &_.c ; \ (???)
: &_.?WL ( w -- ) 1 and &_.WL ;                 \ aus letzem Bit .L\.W anh.

: &_- ( -- ) [ char - ] literal &_c ;           \ Minus anfuegen (???)
: &_( ( -- ) [ char ( ] literal &_c ;           \ Klammer auf anfuegen (???)
: &_) ( -- ) [ char ) ] literal &_c ;           \ Klammer zu anfuegen (???)
: &_, ( -- ) [ char , ] literal &_c ;           \ Komma anfuegen
: &_# ( -- ) [ char # ] literal &_c ;           \ Gatter anfuegen

: &_$n ( addr n -- ) under &px swap cmove &p+ ; \ counted String an Puffer
: &_$ ( addr -- ) count &_$n ;                  \ String an Puffer

: &_t ( n -- ) BEGIN bl &_c dup &pl <= UNTIL drop ; \ tabuliere zu Stelle n
-->



\ (3) Disassembler                                               JPS 04-18-92

: &_l ( l -- ) dup 0< IF &_- THEN               \ Zahl ausgeben
  [ char $ ] literal &_c abs <# #s #> &_$n ;

: &/7 ( w -- c ) 8/ 8/ 8/ $7 and ;              \ shifte 9 mal und filtre
: &_,/7 ( w -- c ) &_, &/7 ;                    \ shifte 9 mal und trenne
: &_,// ( w -- w ) dup 8/ $38 and swap &_,/7 or ; \ (1)
: &_x,/x ( w addr -- )                          \ doppelte Ausgabe m. Shift
  2dup execute swap &_,/7 swap execute ;

: &_Z ( n -- ) [ char 0 ] literal + &_c ;       \ 0-9 ausgeben
: &_rg ( n c -- ) &_c $7 and &_Z ;              \ Register ausgeben

: ~USP ( -- ) " USP" &_$ ;                      \ ankleben USP
: ~l ( -- ) &atx @ 4 &ln +! &_l ;               \ Langwort holen, zaehl., ausg.
: ~qi ( n -- ) w>l &_l ;                        \ Wort ausg.
: ~#qi ( n -- ) &_# ~qi ;                       \ # Wort ausg.
: ~i ( -- ) &atx@+ ~qi ;                        \ Wort holen und ausg.
: ~#b ( -- ) &atx@+ $ff00 &stwa b>l ~#qi ;      \ Byte holen, pr. und ausg.
: ~#w ( -- ) &_# ~i ;                           \ # Wort holen und ausg.
-->



\ (4) Disassembler                                               JPS 04-18-92

: ~#l ( -- ) &_# ~l ;                           \ (1) # Langwort holen und ausg.
: ~#8, ( w -- ) &/7 0 case? IF $8 THEN ~#qi &_, ; \ Quick imm. Data ausg.
: ~lab ( w -- ) [ char L ] literal &_c          \ Label-Adresse ausg.
  &at@ + 2+ >ABS &_l ;
: ~Dn ( n -- ) [ char D ] literal &_rg ;        \ D-Register (???)  ea $0
: ~An ( n -- ) [ char A ] literal &_rg ;        \ A-Register (???)
: ~Rn ( n -- ) dup $8 and IF ~An ELSE ~Dn THEN ; \ A/D-Register
: ~,/Rn ( n -- ) &_,/7 ~Rn ;                    \
: ~/Dn, ( n -- ) dup &/7 ~Dn &_, ;              \
: ~ix ( n -- ) dup 2* ~,/Rn dup $800 and &_.WL  \
  $100 &stwa ( prov. )
  $600 and ?dup IF [ char * ] literal &_c 2 &stfl
  &/7 dup 2 > - 2* &_Z THEN &_) ;

: ~REG ( wm -- ) $38 and $20 = IF -1 $f ELSE 1 0 THEN &atx@+ 2* \ Registerliste
  BEGIN dup WHILE dup $7 and $6 case? IF over ~Rn &_- ELSE 2/ 1 case?
    IF over ~Rn dup $7 > IF [ char / ] literal &_c THEN ELSE drop THEN THEN
    -rot over + rot 2/
  REPEAT 2drop drop ;
-->



\ (5) Disassembler                                               JPS 04-18-92

: ~AnT ( n -- ) &lc @ [ char b ] literal =              \ ea $1 (???)
  IF $4000 &tflg IF &sterr EXIT THEN THEN ~An ;
: ~() ( n -- ) &_( ~An &_) ;                            \ ea $2
: ~()+ ( n -- ) ~() [ char + ] literal &_c ;            \ ea $3
: ~-() ( n -- ) &_- ~() ;                               \ ea $4
: ~d() ( n -- ) ~i ~() ;                                \ ea $5
: ~(,) ( n -- ) &atx@+ dup b>l ~qi swap &_( ~An ~ix ;   \ ea $6
: ~$w ( n -- ) ~i &_.?WL ;                              \ ea $7
: ~$l ( n -- ) ~l &_.?WL ;                              \ ea $8
: ~d[] ( n -- ) drop &atx@+ W>L ~lab " (PC)" &_$ ;      \ ea $9
: ~[,] ( n -- ) drop &atx@+ dup B>L ~lab &_(            \ ea $a
  " PC" &_$ ~ix ;
: ~#im ( n -- ) drop &lc @                              \ ea $b
  [ char l ] literal case? IF ~#l exit THEN             \ (???)
  [ char w ] literal case? IF ~#w exit THEN drop ~#b ;  \ (???)

: &ea ( w m b addr -- )                                 \ ggf. ea ausf.
  -rot and IF execute ELSE drop &sterr THEN ;
-->




\ (6) Disassembler                                               JPS 04-18-92

: &?ea ( w m -- ) over $3f and                          \ Selektion ea's
  $38 case? IF $080 ['] ~$w  &ea EXIT THEN                      \ ea $7
  $39 case? IF $100 ['] ~$l  &ea EXIT THEN                      \ ea $8
  $3a case? IF $200 ['] ~d[] &ea EXIT THEN                      \ ea $9
  $3b case? IF $400 ['] ~[,] &ea EXIT THEN                      \ ea $a
  $3c case? IF $800 ['] ~#im &ea EXIT THEN 8/                   \ ea $b
    0 case? IF    1 ['] ~Dn  &ea EXIT THEN                      \ ea $0
    1 case? IF    2 ['] ~AnT &ea EXIT THEN                      \ ea $1
    2 case? IF $004 ['] ~()  &ea EXIT THEN                      \ ea $2
  $03 case? IF $008 ['] ~()+ &ea EXIT THEN                      \ ea $3
  $04 case? IF $010 ['] ~-() &ea EXIT THEN                      \ ea $4
  $05 case? IF $020 ['] ~d() &ea EXIT THEN                      \ ea $5
  $06 case? IF $040 ['] ~(,) &ea EXIT THEN 2drop &sterr ;       \ ea $6

: ~ea1 ( w -- w ) dup $fff &?ea ;   : ~em9 ( w -- ) $1f4 &?ea ;
: ~ea3 ( w -- ) $ffd &?ea ;         : ~ea5 ( w -- ) $7e4 &?ea ;
: ~em5 ( w -- ) $7ec &?ea ;         : ~ea6 ( w -- ) $1ff &?ea ;
: ~ea7 ( w -- ) $1fd &?ea ;         : ~ea8 ( w -- ) $1fc &?ea ;
-->




\ (7) Disassembler                                               JPS 04-18-92

: &?SR ( w -- addr ) $200 and IF $4 &stfl [ char w ]    \ ggf. privilegiert .w
  literal &lc ! " SR" ELSE " CCR" THEN ;

: &bcd ( w -- ) dup $8 and IF ['] ~-() ELSE ['] ~Dn THEN &_x,/x ;
: &rel ( w -- ) B>L 0 case? IF &atx@+ W>L THEN ~lab ;
: &msy ( w -- ) dup &?SR swap ~ea3 &_, &_$ ;
: &msr ( w -- ) dup not &?SR &_$ &_, ~ea7 ;
: &bts ( w -- ) dup ~#im &_, ~ea3 ;     : &opi ( w -- ) dup ~#im &_, ~ea7 ;
: &xaa ( w -- ) ['] ~An &_x,/x ;        : &cpm ( w -- ) ['] ~()+ &_x,/x ;
: &mov ( w -- ) ~ea1 &_,// ~ea7 ;       : &opa ( w -- ) ~ea1 &_,/7 ~An ;
: &mvq ( w -- ) dup b>l ~#qi ~,/Rn ;    : &xdr ( w -- ) ~/Dn, ~Rn ;
: &dbc ( w -- ) ~Dn &_, &atx@+ ~lab ;   : &stp ( w -- ) drop ~#w ;
: &lea ( w -- ) dup ~ea5 &_,/7 ~An ;    : &pck ( w -- ) &bcd &_, ~#w ;
: &sys ( w -- ) ~#w &_, &?SR &_$ ;      : &lnk ( w -- ) ~An &_, ~#w ;
: &cmp ( w -- ) ~ea1 ~,/Rn ;            : &chk ( w -- ) dup ~ea3 ~,/Rn ;
: &eor ( w -- ) ~/Dn, ~ea7 ;            : &adm ( w -- ) ~/Dn, ~ea8 ;
: &mm5 ( w -- ) dup ~em5 &_, ~REG ;     : &mm9 ( w -- ) dup ~REG &_, ~em9 ;
: &mus ( w -- ) ~An &_, ~USP ;          : &mur ( w -- ) ~USP &_, ~An ;
: &mpm ( w -- ) ~/Dn, ~d() ;            : &clm ( w -- ) ~#b &_, ~ea5 ;
: &mpr ( w -- ) dup ~d() ~,/Rn ;        : &8op ( w -- ) dup ~#8, ~ea6 ;
: &shf ( w -- ) dup $20 and IF &xdr ELSE dup ~#8, ~Dn THEN ;
-->

\ (8) Disassembler                                               JPS 04-18-92

: &^ ( ma ky lt ex "cm" -- )                                \ Tabellenbildner
  swap 2swap swap w, w, w, , bl word drop $8 allot ;

create &tb
  $f000 $a000 $2000 ' ~qi  &^ ALINE       $f000 $f000 $2000 ' ~qi  &^ FLINE
  $f000 $6000 $0080 ' &rel &^ b           $f100 $7000     0 ' &mvq &^ moveq
  $e1c0 $2040 $1000 ' &opa &^ movea       $f0c0 $b0c0 $0100 ' &opa &^ cmpa
  $f0c0 $d0c0 $0100 ' &opa &^ adda        $f0c0 $90c0 $0100 ' &opa &^ suba
  $f1c0 $81c0     0 ' &chk &^ divs        $f1c0 $80c0     0 ' &chk &^ divu
  $f1c0 $c1c0     0 ' &chk &^ muls        $f1c0 $c0c0     0 ' &chk &^ mulu
  $f1f8 $c140     0 ' &xdr &^ exg         $f1c8 $c188     0 ' &xdr &^ exg
  $f1f8 $c148     0 ' &xaa &^ exg
  $f100 $b000 $0010 ' &cmp &^ cmp         $f100 $b100 $0010 ' &eor &^ eor
  $f100 $d000 $0010 ' &cmp &^ add         $f100 $d100 $0010 ' &adm &^ add
  $f100 $9000 $0010 ' &cmp &^ sub         $f100 $9100 $0010 ' &adm &^ sub
  $f1f0 $8100     0 ' &bcd &^ sbcd        $f1f0 $c100     0 ' &bcd &^ adcb
  $f100 $c000 $0010 ' &chk &^ and         $f100 $c100 $0010 ' &adm &^ and
  $f100 $8000 $0010 ' &chk &^ or          $f100 $8100 $0010 ' &adm &^ or
  $f0ff $50fc $000a ' drop &^ trap        $f0fe $50fa $000b ' ~#im &^ trap
-->



\ (9) Disassembler                                               JPS 04-18-92

  $f0f8 $50c8 $0008 ' &dbc &^ db          $f0c0 $50c0 $0008 ' ~ea7 &^ s
  $f100 $5000 $0010 ' &8op &^ addq        $f100 $5100 $0010 ' &8op &^ subq
  $f130 $d100 $0010 ' &bcd &^ addx        $f130 $9100 $0010 ' &bcd &^ subx
  $ffb8 $4880 $0040 ' ~Dn  &^ ext         $fff8 $49c0 $0002 ' ~Dn  &^ extb
  $fff8 $4840     0 ' ~Dn  &^ swap        $f138 $b108 $0010 ' &cpm &^ cmpm
  $ff80 $4880 $0040 ' &mm9 &^ movem       $ff80 $4c80 $0040 ' &mm5 &^ movem
  $ff00 $0400 $0010 ' &opi &^ subi        $f1b8 $0188 $0040 ' &mpm &^ movep
  $f1b8 $0108 $0040 ' &mpr &^ movep       $fff8 $4e60 $0004 ' &mus &^ move
  $fff8 $4e68 $0004 ' &mur &^ move        $fdc0 $40c0     0 ' &msr &^ move
  $fdc0 $44c0     0 ' &msy &^ move        $ffff $4afc $2000 ' drop &^ illegal
  $ffc0 $4ac0     0 ' ~ea7 &^ tas         $ff00 $4a00 $0010 ' ~ea7 &^ tst
  $ff00 $4400 $0010 ' ~ea7 &^ neg         $ff00 $4000 $0010 ' ~ea7 &^ negx
  $ff00 $4600 $0010 ' ~ea7 &^ not         $ff00 $4200 $0010 ' ~ea7 &^ clr
  $f1f0 $8140 $0002 ' &pck &^ pack        $f1f0 $8180 $0002 ' &pck &^ unpk
  $fff0 $06c0 $0002 ' ~Rn  &^ rtm         $ffc0 $06c0 $0002 ' &clm &^ callm
  $ffff $4e73 $0004 ' drop &^ rte         $ffff $4e75     0 ' drop &^ rts
  $ffff $4e77     0 ' drop &^ rtr         $ff00 $0600 $0010 ' &opi &^ addi
  $f1c0 $4180     0 ' &chk &^ chk         $ffc0 $0840     0 ' &opi &^ bchg
  $f1c0 $0140     0 ' &eor &^ bchg        $ffc0 $0880     0 ' &opi &^ bclr
-->



\ (A) Disassembler                                               JPS 04-18-92

  $f1c0 $0180     0 ' &eor &^ bclr        $ffc0 $08c0     0 ' &opi &^ bset
  $f1c0 $01c0     0 ' &eor &^ bset        $ffc0 $0800     0 ' &bts &^ btst
  $f1c0 $0100     0 ' &chk &^ btst        $ffbf $0a3c     0 ' &sys &^ eori
  $ffbf $023c     0 ' &sys &^ andi        $ffbf $003c     0 ' &sys &^ ori
  $ff00 $0c00 $0010 ' &opi &^ cmpi        $ff00 $0200 $0010 ' &opi &^ andi
  $ff00 $0000 $0010 ' &opi &^ ori         $ff00 $0a00 $0010 ' &opi &^ eori
  $c000 $0000 $0200 ' &mov &^ move        $f018 $e000 $0030 ' &shf &^ as
  $f018 $e008 $0030 ' &shf &^ ls          $f018 $e018 $0030 ' &shf &^ ro
  $f018 $e010 $0030 ' &shf &^ rox         $fec0 $e0c0 $0020 ' ~ea8 &^ as
  $fec0 $e2c0 $0020 ' ~ea8 &^ ls          $fec0 $e6c0 $0020 ' ~ea8 &^ ro
  $fec0 $e4c0 $0020 ' ~ea8 &^ rox         $ffc0 $4800     0 ' ~ea7 &^ nbcd
  $ffc0 $4ec0     0 ' ~ea5 &^ jmp         $ffc0 $4e80     0 ' ~ea5 &^ jsr
  $fff8 $4848 $0002 ' ~#qi &^ bkpt
  $fff0 $4e40     0 ' ~#qi &^ trap        $f1c0 $41c0     0 ' &lea &^ lea
  $ffc0 $4840     0 ' ~ea5 &^ pea         $fff8 $4e50     0 ' &lnk &^ link
  $fff8 $4e58     0 ' ~An  &^ unlk        $ffff $4e71     0 ' drop &^ nop
  $ffff $4e76     0 ' drop &^ trapv       $ffff $4e70 $0004 ' drop &^ reset
  $ffff $4e72 $0004 ' &stp &^ stop        $ffff $4e74 $0002 ' ~#im &^ rtd
  $0000 $0000 $2000 ' ~qi  &^ DC                            \ Ende Tabelle
-->



\ (B) Disassembler                                               JPS 04-18-92

: &ln67 ( w -- c ) dup $c0 and                      \ Laengensuffixe Bits 6-7
  $80 case? IF [ char l ] literal EXIT THEN         \ (???)
  $40 case? IF [ char w ] literal EXIT THEN
  $00 case? IF [ char b ] literal EXIT THEN &sterr bl ;

: &lnmv ( w -- c ) dup $3000 and                    \ Laengensuffixe fuer move
  $2000 case? IF [ char l ] literal EXIT THEN
  $3000 case? IF [ char w ] literal EXIT THEN
  $1000 case? IF [ char b ] literal EXIT THEN &sterr bl ;

: &wls ( m w -- ) and IF [ char l ] literal         \ .l/.w Suffix
  ELSE [ char w ] literal THEN &_.x ;

: &rl8 ( w -- w ) dup $100 and IF [ char l ] literal
  ELSE [ char r ] literal THEN &_c ;

: &_cc ( w addr -- w ) over 8/ 8/ 2/ 1 or + dup     \ kopiere cc Suffix-String
  c@ &_c 1+ c@ &_?c $f0ff and ;                     \ Kondition ausblenden
-->




\ (C) Disassembler                                               JPS 04-18-92

: &sc ( w -- w ) " t f hilscccsneeqvcvsplmigeltgtle" &_cc ;     \ set-cond.
: &bc ( w -- w ) " rasrhilscccsneeqvcvsplmigeltgtle" &_cc       \ branch-cond.
  dup $ff and IF [ char s ] literal &_.c THEN ;                 \ ggf. Suffix

: &opcd ( wm tbl -- wm ) $8 &_t $a + &_$            \ tabuliere, kopiere Bef.
  $0020 &tflg IF &rl8 THEN
  $0010 &tflg IF &ln67 &_.x THEN
  $0200 &tflg IF &lnmv &_.x THEN
  $0008 &tflg IF &sc THEN
  $0141 &tflg ?dup IF over &wls THEN
  $1000 &tflg ?dup IF over not &wls THEN
  $0080 &tflg IF &bc THEN
  $10 &_t ;                                         \ trenne Befehlswort ab

: &comm ( -- ) $2006 &tflg IF $4a &_t [ char [ ] literal &_c    \ ggf. Flags
  $2000 &tflg IF [ char ? ] literal &_c THEN
  $0004 &tflg IF [ char P ] literal &_c THEN
      2 &tflg IF [ char + ] literal &_c THEN
  [ char ] ] literal &_c THEN ;                                 \ in []
-->



\ (D) Disassembler                                               JPS 04-18-92

: &code ( -- ) $2f &_t [ char ; ] literal &_c                   \ Ausgabe Worte
  &ln @ &ln off $a min 0 DO &atx@+ <# # # # # #> &_$n bl &_c 2 +LOOP ;

: &mnmo ( w tbl -- ) dup cell+ w@ &fl ! &at@ >ABS &_l
  under w@ not and over &opcd swap $6 + @ execute ;

: dslx ( -- ) base @ hex [ &tb $12 - ] literal
  BEGIN pad off &lc off &ln off &atx@+ swap
    BEGIN $12 + 2dup w@ and over 2+ w@ = UNTIL
    under &mnmo $8000 &tflg WHILE
  REPEAT drop &code &comm &atx &at! base ! ;

: &?at! ( addr -- ) depth 0= IF abort" where?" THEN &at! ;

: ddis ( addr -- ) &?at!                                \ disassembl. Data
  BEGIN cr stop? 0= WHILE dslx pad count type REPEAT ;

: dis ( addr -- ) CODE>DATA ddis ;                      \ disassembl. Code

: adis ( addr -- ) (ABS) ddis ;                         \ disassembl. Absolut
