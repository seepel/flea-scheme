\ +====================================================================+
\  Delimited Continuations
\ +====================================================================+
\  This is a delimited contminuation library implemented in WAForth, a
\  Forth dialect for WebAssembly.

: RESET ( xt -- ) 
\  Places a delimiter and executes the xt.
\  The an xt representing the continuation of the next word is left on
\  the return stack. This ensures that the xt will be executed if the
\  continuation is never executed.
;

: SHIFT ( xt -- )
\  Captures the current continuation to the next delimiter and
\  executes the xt with the captured continuation at the top of the
\  stack.
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
   ['] NOCAPTURE RESET 
;

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
                                \   execute the xt
   + 1 -                        \ * This will be captured and dropped
                                \   as the continuation is never
                                \   executed
;

: TEST-DISCARD ['] DISCARD ;
TEST-DISCARD .  \ Prints 10


\ +--------------------------------------------------------------------+
\  Example 3: Continuation is captured
\ +--------------------------------------------------------------------+

: NOOP ;           \ * We'll leave the continuation
                   \   xt on the stack
: CAPTURE-EXAMPLE  
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

