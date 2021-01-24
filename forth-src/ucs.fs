\ Universal Control Structures (UCS) Loadscreen                   JPS 09-23-91
\ by  Kevin Haddock, Chico, California
\ Published in FD vol. XIII, no. 3, September '91
\ Ported to F68K by Joerg Plewe, Muelheim, Germany in September '91

#ifndef UCS     : UCS ;
forget  UCS
vocabulary UCS

UCS also definitions  forth



decimal
2 6 thru




toss forth

\ tests and examples
\ 7 load
\ 8 load

\ UCS                                                             JPS 09-23-91

: @char ( -- char )
    blk @ IF  sblk @ block  ELSE  tib  THEN
    >in @ + c@ ;
\ returns the next char in the input stream

: account ( cnt char -- cnt' )
    dup [char] { =
    IF    drop 1+
    ELSE  [char] } =
        IF  1-  THEN
    THEN ;
\ given the current level count and char returns the adjusted count

: >next ( n -- )
    0< IF  1  ELSE  -1  THEN  >in +! ;
\ adjusts te input stream pointer based on the sign of the given number







\ UCS                                                             JPS 09-23-91

: i~ ( n -- )
    -1 >in +!                     \ 1 >next
    dup 0< IF  1  ELSE  -1  THEN
    BEGIN
        @char account 2dup = 0=
    WHILE
        over >next
    REPEAT  2drop ;
\ adjusts the interpretative pointer to the given control block. Negative
\ control block offsets move the pointer forward. Positive or zero backward

: i&| ( -- )
    1 BEGIN
        @char account ?dup
    WHILE
        -1 >next
    REPEAT
    1 >in +! ;   \ -1 >next
\ adjust the interpretative pointer to the end of the current control block.




\ UCS                                                             JPS 09-23-91
\ : >mark  cp @  0 code, ;       : <mark     cp @ ;
\ : >resolve   cp @ swap ! ;     : <resolve  code, ;
forth definitions
: { ( -- addr 0 )
    state @  IF  <mark  0  THEN ;   immediate
\ denotes the start of a control block

: & ( addr -- addr )
    state @
    IF
        postpone ?branch <mark  swap <resolve
    ELSE
        0= IF  i&|  THEN
    THEN ;   immediate
\ falls out of the current control block if given value is FALSE

: | ( addr -- addr )
    state @
    IF
        postpone 0=  postpone &
    ELSE
        IF  i&|  THEN
    THEN ;   immediate
\ falls out of the current control block if given value is TRUE
\ UCS                                                             JPS 09-23-91

: } ( addr addr -- )
    state @
    IF
        BEGIN   ?dup
        WHILE
                dup code@ swap
                >resolve
        REPEAT drop
    THEN ;    immediate
\ marks the end of a control block. When compiling, this word resolves
\ all the forward references for this block.












\ UCS                                                             JPS 09-22-91

: ~ ( a1 a2 a1 a2 .... n -- )
    state @
    IF
        postpone branch
        >r  sp@ r@ abs cells 2* +    \ find stackposition
        r>  0<                          \ up or down?
        IF
            <mark over @ <resolve  swap !   \ change the stack!!
        ELSE
            cell+ @ <resolve
        THEN
    ELSE  i~  THEN ;  immediate

UCS definitions forth
: ~: ( n -- )       Create  ,  Does>  @ postpone ~ ;

: ~S: ( e s -- )    DO  i ~:  immediate  LOOP ;

forth definitions
5 -4 ~S: vvvvv vvvv vvv vv  ^ ^^ ^^^ ^^^^ ^^^^^



\ Tests for UCS                                                   JPS 09-23-91
decimal
16 constant wide
: .char ( char -- )
    { { dup 32 < |  dup 126 >  | vv }
    drop [char] . } emit ;
: .asc
    { over c@ .char 1- swap 1+ swap ?dup & ^ } drop ;

: .hex      base push hex
    { over c@ <# # # #> type space 1- swap 1+ swap ?dup & ^ } drop ;
: .adr
    base @ { { dup 10 = & [char] & vv }
             { dup 16 = & [char] $ vv }
             $20
           } emit drop
    <# [char] : hold # # # # # # # # # # #> type space ;
: .dump
    { { key? | cr  over .adr 2dup .hex space .asc  vv } cr abort } ;
: dump
    >r { r@ & { wide r@ over > | drop r@ }
    r@ min r> over - >r 2dup .dump + ^ } r> 2drop cr ;



\ Tests for UCS                                                   JPS 09-23-91
\ interpretative:

{ {
    cr .( Enter a letter: ) key  $7f and
    { dup 27 =  &  .( bye!) vvv }
    { dup char a =  &  .( alpha) vv }
    { dup char e =  &  .( edward) vv }
    { dup char i =  &  .( ida)    vv }
    { dup char o =  &  .( ocean) vv }
    { dup char u =  &  .( union) vv }

    cr bell dup emit .(  is not a vowel!)
  } drop ^
} drop
