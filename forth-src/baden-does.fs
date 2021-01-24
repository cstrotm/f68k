\ ******************************************                      JPS 11-29-90
\ :DOES>   von Wil Baden
\ ******************************************


: (does>
     [ assembler ] CODEhere [ forth ]  >r postpone does> ;

: :does>
    last @ 0= abort" <without reference>"
    (does> ] ;
