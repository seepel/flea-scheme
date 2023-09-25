\ +====================================================================+
\  Delimited Continuations
\ +====================================================================+
\  This is a delimited contminuation library implemented in WAForth, a
\  Forth dialect for WebAssembly.

: NOOP ;

3 CELLS BUFFER: CURRENT-DELIMITER 

: DELIMITER@SP ( -- sp ) CURRENT-DELIMITER           @ ;
: DELIMITER@RP ( -- rp ) CURRENT-DELIMITER 1 CELLS + @ ;
: DELIMITER@XT ( -- xt ) CURRENT-DELIMITER 2 CELLS + @ ;

: DELIMITER!SP ( sp -- ) CURRENT-DELIMITER           ! ;
: DELIMITER!RP ( rp -- ) CURRENT-DELIMITER 1 CELLS + ! ;
: DELIMITER!XT ( xt -- ) CURRENT-DELIMITER 2 CELLS + ! ;

: DELIMITER! ( sp rp xt -- )
   DELIMITER!XT
   DELIMITER!RP
   DELIMITER!SP
;

: POPDELIMITER ( r: xt-1 sp-1 rp-1 -- )
\  Resets the current delimiter to the outer delimiter.
\ 
\  The execution token for this word will be left on the return stack
\  in order to reset the current delimiter to the outer delimiter.
   2R> R>      ( sp rp xt )
   DELIMITER!  ( )
;

: DELIMITER>R ( r: -- xt sp rp [pop] )
\  Saves the current delimiter to the return stack leaving 
\  ['] POPDELIMITER on the stack afterwards so that it can clean up.
   DELIMITER@XT                   >R   ( r: -- xt )
   DELIMITER@SP DELIMITER@RP     2>R   ( r: -- xt sp rp )
   ['] POPDELIMITER               >R   ( r: -- xt sp rp [pop] )
;

: PUSHDELIMITER ( xt -- ) ( r: -- xt sp-1 rp-1 [pop] )
\  Pushes a new delimiter onto the stack given an xt.
   \ Save the current delimiter to the return stack
   DELIMITER>R       ( r: sp rp xt [pop] )
   SP@ SWAP RP@ SWAP  ( sp rp xt )
   DELIMITER!        ( )
;

: INITDELIMITER
   SP@ RP@ 2>R 
   ['] NOOP ['] POPDELIMITER 2>R
; INITDELIMITER


: RESET ( xt -- ) 
\  Executes the xt within a delimited continuation
\  An xt representing the continuation of the next word is left on
\  the return stack. This ensures that the xt will be executed if the
\  continuation is never executed.
   ['] ; EXECUTE
   ['] :NONAME
; 

0 CONSTANT K-XT-OFFSET
1 CELLS CONSTANT K-STACK-SIZE-OFFSET
2 CELLS CONSTANT K-RETURN-SIZE-OFFSET
3 CELLS CONSTANT K-DATA-OFFSET

: K@XT          ( *k -- xt )                          @ ;
: K@STACK-SIZE  ( *k -- n  )   K-STACK-SIZE-OFFSET  + @ ;
: K@RETURN-SIZE ( *k -- n  )   K-RETURN-SIZE-OFFSET + @ ;
: K@DATA        ( *k -- *d )   K-DATA-OFFSET        + @ ;

: K!XT          ( xt *k -- )                          ! ;
: K!STACK-SIZE  ( n  *k -- )   K-STACK-SIZE-OFFSET  + ! ;
: K!RETURN-SIZE ( m  *k -- )   K-RETURN-SIZE-OFFSET + ! ;


: K@S ( *k -- *ks n )
   DUP K@STACK-SIZE SWAP   ( n *k )
   K@DATA                    ( n *ks )
   SWAP                      ( *ks n )
;

: K@R ( *k -- *kr m )
   DUP K@RETURN-SIZE SWAP  ( m *k )
   DUP K@DATA SWAP         ( m *ks *k)
   K@STACK-SIZE +          ( m *kr )
   SWAP
;

: CAPTUREKSTACKS ( ...xn -- k* ) ( r: ...ym -- ) ( c: -- xt )
   \ Calculate the size of the continuation
   SP@ DELIMITER@SP -    ( n )
   RP@ DELIMITER@RP -    ( n m )
   2DUP + 2 CELLS +      ( n m ksz )
   \ Allocate the space for the continuation
   ALLOCATE          ( n m *k )
   \ OOPS! We ran out of memory
   DUP 0= IF
      \ TODO: Expand error handling system
      ." Out of memory " CR
      ABORT
   THEN                  ( n m *k )
   \ Write the stack sizes
   TUCK K!RETURN-SIZE    ( n *k )
   TUCK K!STACK-SIZE     ( *k )
   \ Get the delimiter's stack pointer
   DELIMITER@SP          ( *k sp-1 )
   \ Get the continuation's stack
   OVER K@S              ( *k sp-1 *ks n )
   \ Copy the stack to the continuation
   MOVE                  ( *k )
   \ Get the delimiter's return stack pointer
   DELIMITER@RP          ( *k rp )
   \ Get the continuation's return stack
   K@R                   ( *k rp *kr m )
   \ Copy the return stack to the continuation
   MOVE                  ( *k )
;

: CAPTUREKXT ( c: -- xt )
\  Captures a continuation and returns a pointer to the continuation as
\  well as an xt that represents the continuation. 
\ 
\  Note: The xt is left on the _compile_ time stack.
   ['] ;       EXECUTE
   ['] :NONAME EXECUTE
   LATEST
; IMMEDIATE

: RESUMEK ( *k -- ...ks *k ) ( r: ...kr -- )
\  Sets up the the xt stored in the continuation to be invoked
\  saved inside the continuation
   \ We're basically going to set everything up and then copy the
   \ stacks back in to place. Need to be super careful that the stacks 
   \ aren't messed with at the wrong time by other code.

   \ First we'll advance the stack pointer so that we don't operate in
   \ in the area we are about to write to. 

   \ Save the continuation and current stack pointer away on the return
   \ stack
   DUP >R         ( *k )                  ( r: *k )

   \ Get the size of the continuation's stack 
   K@STACK-SIZE   ( n )

   \ Calculate the next stack pointer
   SP@ SWAP -     ( sp' )

   \ Set the stack pointer to make room for the continuation's stack
   \ Right now the area between sp and sp' is garbage
   SP!            ( ...garbage )             ( r: *k )

   \ We'll grab the string for the continuation's stack
   R@ K@S         ( ...garbage *ks n )       ( r: *k )
   SP@ OVER -     ( ...garbage *ks n sp )
   SWAP MOVE      ( ...ks )
   
   \ Now we'll copy the return stack
   \ Let's grab the string for the return stack
   R> DUP K@R     ( ...ks *k kr m )         
   RP@ SWAP       ( ...ks *k kr rp m )      
   2DUP -         ( ...ks *k kr rp m rp' )  
   RP!            ( ...ks *k kr rp m )      
   MOVE           ( ...ks *k )               ( r: ...kr )

;

: DROPK ( *k -- ) FREE ;

: SHIFT ( <dlm> ...ks xt -- <dlm> ... ) ( r: <dlm> ...kr -- <dlm> )
\ Captures the current continuation to the next delimiter and leaves
\ it on the top of the stack before executing the provided xt.
   CAPTUREKSTACKS    ( <dlm> *k )      ( r: <dlm> )
   \ SHOOT, we need an instruction pointer in order to capture the
   \ continuation. WAForth is subroutine threaded, meaning it uses
   \ wasm's instruction pointer which for security reason's isn't
   \ accessible to user.

   \ We can probably reuse most of WAForth to implement this. We can
   \ leave the core words as is and modify DOCOLON to compile new words
   \ as indirectly threaded words.

;  


\ +====================================================================+
\  Examples
\ +====================================================================+

\ +--------------------------------------------------------------------+
\  Example 1: Continuation is never executed
\ +--------------------------------------------------------------------+

: NOCAPTURE-EXAMPLE  \ * We'll just execute normally, nothing special
   3                 \ * Push 3 onto the stack this will eventually be captured
                     \   to the continuation stack
   5 2 *             \ * In the next examples we'll replace this with a call to
                     \   SHIFT
   + 1 -             \ * This will be the rest of the continuation and captured
                     \   inside an xt
;

: TEST-NOCAPTURE
   ['] NOCAPTURE-EXAMPLE RESET 
;

:NONAME ." Example 1: " CR ; EXECUTE
TEST-NOCAPTURE .     \ Print 3 + 5 * 2 - 1 = 12

\ +--------------------------------------------------------------------+
\  Example 2: Continuation is discarded
\ +--------------------------------------------------------------------+

: JUSTRETURNTEN DROPK 5 2 * ;   \ * We'll just return 10
: DISCARD-EXAMPLE               \ * We'll drop the rest of the
                                \   continuation
   3                            \ * Push 3 onto the stack this will
                                \   eventually be captured and dropped
   ['] JUST-RETURN-TEN          \ * This will be executed when we
                                \   capture the continuation
   SHIFT                        \ * Capture the continuation and
      + 1 -                        \ * This will be captured and dropped
   <SHIFT                             \   as the continuation is never
                                \   executed
;

: TEST-DISCARD ['] DISCARD-EXAMPLE RESET ;
TEST-DISCARD .  \ Prints 10


\ +--------------------------------------------------------------------+
\  Example 3: Continuation is captured
\ +--------------------------------------------------------------------+

: NOOP ;           \ * We'll leave the continuation
                   \   xt on the stack
: CAPTURE-EXAMPLE  
   RESET
      3               \ * Push 3 onto the stack this
                      \   will eventually be captured
                      \   and restored when the 
                      \   continuation is executed
      ['] NOOP        \ * Do nothing leave the xt on     <-- This is a 
                      \   the stack                          hole that we
      SHIFT           \ * Capture the continuation and   <-- can fill in
                      \                                      later
                      \   execute the noop xt
      + 1 -           \ * This will be executed when
                   \   the continuation is invoked
;

: TEST-CAPTURE ['] CAPTURE-EXAMPLE RESET ;
\ Instead of a result, we get the xt representing the continuation
TEST-CAPTURE .           \ Prints <xt>

\ We can invoke the continuation by executing the xt
10 TEST-CAPTURE EXECUTE  \ Prints 12

\ We can also invoke the continuation multiple times.
TEST-CAPTURE
10 OVER EXECUTE          \ Prints 3 + 10 12
5 2 * OVER EXECUTE       \ Prints 12
3 OVER EXECUTE           \ Prints 5

