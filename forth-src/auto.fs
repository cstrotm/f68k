\ Include first AUTOINCLUDE                                        cep 20Mai91

also blockstream

Stream: first.file

: init.system    empty-buffers
    key? IF  ." No file executed" cr  exit THEN
    first.file " 0:AUTOINCLUDE" bUse
    IF  ." No file 0:AUTOINCLUDE" cr  exit THEN
    first.file bselect drop  1 load ;

: cold.start ( -- )   init.system [ (cold) @ ] Literal execute ;

' cold.start (cold) !

toss

\\
The file AUTOINCLUDE in the root directory of drive 0 is INCLUDEd automatically
when starting F68K. Press a key to prevent this, if executing AUTOINCLUDE would
produce a crash.
