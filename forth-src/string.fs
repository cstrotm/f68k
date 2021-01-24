\                                                                 DK 1991oct06
cr .( Die Stringverarbeitung )

80 constant c/s                 \ Standard-Anzahl Buchstaben pro String

: STRING ( maxLength -- )
  create 0 c, allot ;           \ Use: 80 string TEST$

: READ"  ( adr -- )             \ Use: TEST$ READ" Testing!"
  local adr
  34 word count dup adr c!
  adr 1+ swap cmove ;

: .$    ( adr )     count type ;
