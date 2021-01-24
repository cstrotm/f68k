\ LIST>                                                           JPS 02-04-91
\ executing some code for all elements of a linked list
\ Seen in 'Vierte Dimension VI/No. 4, Dec.90' in a letter from Bernd Paysan
\ https://forth-ev.de/wiki/res/lib/exe/fetch.php/vd-archiv:4d1990_4.pdf

\ : >LIST                   \ can be replaced with EXECUTE in F68K
\     >r ;  restrict        \ because of native code compilation

: LIST> ( thread -- )
    BEGIN
        @ dup                           \ link to next element
    WHILE                               \ until end-of-list is reached
        dup r@ execute ( >LIST )        \ execute code following call of LIST>
    REPEAT    drop rdrop ;  restrict


\ test:
\ : words
\    wrap  context @                \ set terminal mode and get start-of-list
\    LIST>                          \ execute following code for each element
\    4+ count type space ;          \ this is to be executed
