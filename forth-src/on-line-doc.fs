\ On-Line Documentation, Loadscreen                               JPS 02-17-91

\needs on-line-documentation    : on-line-documentation ;
forget on-line-documentation

Vocabulary on-line-documentation

current @  also  on-line-documentation definitions





2 3 thru

documentation on




toss current !




\ On-Line Documentation, compiling                                JPS 02-17-91

Variable documentation

: set-doc-bit ( -- )
    last @ dup w@  $8000 or  swap w! ;

: compile-doc ( straddr -- )
    count -trailing  dup rot 1- c!   1+ ( countbyte )  allot ;

: (D
    [char] ) word   compile-doc
    set-doc-bit ;


current @  also forth definitions
: ( ( -- )
    last @ 2+ @ cp @ =
    documentation @ 0<>  and
    IF (D  ELSE  postpone ( THEN ;       immediate

toss current !



\ On-Line Documentation, retrieval                                JPS 02-17-91

: ?doc-bit ( h'-addr -- flag )
    2- w@ $8000 and ;

: >doc ( h'-addr -- docaddr )
    cell+ ( cfa ) cell+ ( lfa )
    count + even ;


: .doc ( h'-addr -- )
    >doc [char] ( emit space count type space [char] ) emit ;



current @  also forth definitions
: sc ( -- )
    h' dup ?doc-bit IF .doc  ELSE drop ." no documentation!" THEN ;


toss current !
