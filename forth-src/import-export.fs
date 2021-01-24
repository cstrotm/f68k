\ EXPORT                                                          JPS 10-17-90
Create CR/LF  $d c, $a c,
: write_cr
    CR/LF 2 writesys drop ;

: (export ( blk -- )
    block &2000 0
    DO
       i over + 80 -trailing writesys drop ( does not fail )
       write_cr
    80 +LOOP drop ;

: export ( from to -- )
    1+ swap DO i (export LOOP ;











\ IMPORT
: >space ( addr -- addr )       $20 over c! ;

: decode ( addr -- next-addr flag )
    dup c@ 13 =  IF >space  -1 exit THEN
    dup c@ $20 < IF >space   0 exit THEN        1+ 0 ;

: readln ( addr -- )
    BEGIN dup 1 readsys 0= >R decode R> or UNTIL drop ;

: (import ( blk -- )
    buffer dup &2000 $20 fill              \ delete buffer
    &25 0 DO
            dup readln
            80 +
    LOOP update drop ;

: im-error ( f -- )  0<> abort" something went wrong ... aborted!" ;

: import ( nr -- )  \ import <name>
    stream push  xx stream !
    dup name dup 1 bcreate im-error   xx swap bUse im-error
    xx 1 rot 1- bextend im-error
    1+ 1 DO i (import LOOP ;
