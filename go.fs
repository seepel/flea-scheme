\ +===================================================================+
\  Coroutine and Channel System
\ +===================================================================+
\ 
\ This module implements a primitive coroutine and channel system in
\ Forth inspired by the Go programming language. 
\ 
\ - Coroutines: These are light-weight cooperative multitasking units.
\               A coroutine can be paused and resumed. 
\ 
\ - Channels: These are communication mechanisms that allow coroutines
\             to communicate with each other by sending and receiving 
\             values. They're implemented as circular buffers.
\ 
\ Assumptions and requirements:
\ 1. For now, the implementation contains placeholders for certain 
\    functions like YIELD, SELECT, etc.
\ 2. Channels are designed for byte and cell operations. Make sure to 
\    use the correct operation for the data type.
\ 3. The coroutine scheduler and dispatch mechanism is yet to be 
\    implemented. Placeholders indicate potential expansion points.
\ 
\ TODO: 
\ 1. Replace placeholders with real implementations.
\ 2. Add synchronization for concurrent access.
\ 3. Implement coroutine scheduler and dispatcher.
\ 
\ For a deeper understanding of each function, refer to the specific 
\ function's documentation.

\ +===================================================================+
\  Coroutines
\ +===================================================================+


: GO ( xt -- )
\ Creates a new coroutine that runs the given word or lambda
   RESET 
;

\ +===================================================================+
\  Channels 
\ +===================================================================+

\ +---------------------+ <- CHN start
\ | # of items in buffer|
\ +---------------------+ 
\ | max  items in buffer|
\ +---------------------+ <- HeaDeR start
\ | Start of buffer     |
\ +---------------------+
\ | End of buffer       |
\ +---------------------+
\ | Read pointer        |
\ +---------------------+
\ | Write pointer       |
\ +---------------------+ <- HeaDeR end BUFfer start
\ |                     |
\ | Buffer data...      |
\ |                     |
\ +---------------------+
\

0 CONSTANT CHN@CNT-OFFSET
1 CELLS CONSTANT CHN@MAX-OFFSET

\ Header fields
2 CELLS CONSTANT CHN@BUF-STRT-OFFSET
3 CELLS CONSTANT CHN@BUF-END-OFFSET
4 CELLS CONSTANT CHN@RD-OFFSET
5 CELLS CONSTANT CHN@WRT-OFFSET

\ Header size
4 CELLS CONSTANT CHN-HDR-SZ 

: CHAN ( n -- ) 
\ Create a new named channel structure in the dictionary n bytes large.
   \ Initial size is zero
   CREATE 0 ,          ( n )   
   \ Maximum size is n
   DUP ,               ( n )   
   \ Start of Header
   HERE                ( n *hdr ) 
   \ Start of buffer
   CHN-HDR-SZ +        ( n *b1 )
   \ Start of buffer
   DUP ,               ( n *b1 )
   \ End of buffer 
   2DUP + ,            ( n *b1 )
   \ Read pointer
   DUP ,               ( n *b1 )
   \ Write pointer
   ,                   ( n )
   ALLOT               ( )
  DOES> 
;


\ Simple field accessors
: CHN@CNT@       ( *chn -- # )        CHN@CNT-OFFSET      +  @           ;
: CHN@BUF-STRT@  ( *chn -- *buf1 )    CHN@BUF-STRT-OFFSET +  @           ;
: CHN@BUF-END@   ( *chn -- *bufn )    CHN@BUF-END-OFFSET  +  @           ;
: CHN@RD-PTR@    ( *chn -- *rptr )    CHN@RD-OFFSET       +  @           ;
: CHN@WRT-PTR@   ( *chn -- *wptr )    CHN@WRT-OFFSET      +  @           ;

: CHN!CNT       ( n     *chn -- )     CHN@CNT-OFFSET      +  !           ;
: CHN!RD-PTR    ( *rptr *chn -- )     CHN@RD-OFFSET       +  !           ;
: CHN!WRT-PTR   ( *wptr *chn -- )     CHN@WRT-OFFSET      +  !           ;

: CHN!CNT+      ( *chn -- )           CHN@CNT@            1+ CHN!CNT    ;
: CHN!CNT-      ( *chn -- )           CHN@CNT@            1- CHN!CNT    ;

:CHN!RST-RD-PTR ( *chn -- )
\ Reset the read pointer to the start of the buffer
   CHN@BUF-STRT@ CHN!RD-PTR
;

: CHN!RST-WRT-PTR ( *chn -- )
\ Reset the write pointer to the start of the buffer
   CHN@BUF-STRT@ CHN!WRT-PTR
;

: CHN!RD-PTR+   ( *c -- )
\ Increment the read pointer looping back to the start of the buffer
   DUP CHN@RD-PTR@ 1+          ( *c *r+1)
   DUP CHN@BUF-END@ = IF       ( *c *r+1=end )
      DROP                     ( *c )
      CHN!RST-RD-PTR           ( *c *buf )
   ELSE                        ( *c *r+1<end )
      SWAP CHN!RD-PTR          ( )
   THEN
;

: CHN!WRT-PTR+ ( *chn -- )
\ Increment the write pointer looping back to the start of the buffer
   DUP CHN@WRT-PTR@ 1+         ( *c *w+1)
   DUP CHN@BUF-END@ = IF       ( *c *w+1=end )
      DROP                     ( *c )
      CHN!RST-WRT-PTR          ( *c *buf )
   ELSE                        ( *c *w+1<end )
      SWAP CHN!WRT-PTR         ( )
   THEN
;

\ Closes a channel
: CHN!CLOSE ( *chn -- )
  \ Set the channel's count to -1 to indicate it is closed
   -1 SWAP CHN!CNT
   ." Channel closed" CR
;

\ Is this channel closed?
: CHN@CLOSED? ( *chn -- flag ) CHN@CNT@ -1 = ;

: CHN!WRITE ( x *chn -- )
\ Write a byte the validated write pointer and increment it
   \ Store the value at the write pointer
   TUCK CHN@WRT-PTR@ C!    ( *chn )
   \ Increment the write pointer
   DUP CHN!WRT-PTR+        ( *chn )
   \ Increment the count
   CHN!CNT+                ( )
;

: CHN!READ ( *chn -- x )
\ Read a byte from the validated read pointer and increment it
   \ Retrieve the value at the read pointer
   DUP CHN@RD-PTR@ C@     ( *chn x )
   \ Increment the read pointer
   SWAP DUP CHN!RD-PTR+   ( x *chn )
   \ Decrement the count
   CHN!CNT-               ( x )
;


: CHN@FULL? ( *chn -- flag )
\ Is this channel full for a byte?
   DUP CHN@CNT@ CHN@MAX@ = 
;

: CHN@CMPTY? ( *chn -- flag )
\ Is this channel empty for a byte?
   DUP CHN@CNT@ 0 =
;

: CHN@COUNT CHN@CNT@ ;
: CHN@MAX CHN@MAX@ ;

: C!> ( x *chn -- )
\ Sends a byte to a channel
\ If the channel's buffer is full then the coroutine will block
   CHN@CLOSED? IF
      ABORT" Channel closed"
   ELSE
      CHN@CFULL? WHILE
         ." Channel full" CR
         ['] YIELD SHIFT
      REPEAT CHN!WRITE
   THEN
;

: C?< ( *chn -- x )
\ Receives a byte from a channel
\ If the channel's buffer is empty then the coroutine will block
   CHN@CLOSED? IF
      ABORT" Channel closed" CR
   ELSE
      CHN@CEMPTY? WHILE
         ." Channel empty" CR
         ['] YIELD SHIFT
      REPEAT CHN!READ
   THEN
;

: !> ( x *chn -- )
\ Sends a cell to a channel
\ If the channel's buffer doesn't have room for a cell then the
\ coroutine will block
   \ The magic of continuations. The coroutine will block
   \ until the loop writes all bytes
   SWAP 1 CELLS 0 DO     ( )
      2DUP 255 AND C!>   ( *chn x )
      8 RSHIFT           ( *chn x>>8 )
   LOOP                  ( *chn 0 )
   2DROP                 ( )
;

: ?< ( *chn -- x )
\ Receives a value from a channel
\ If the channel's buffer is empty then the coroutine will block
   0 1 CELLS 0 DO     ( )
      8 LSHIFT        ( *chn x<<8  )
      C@<             ( *chn x<<8 c )
      AND             ( *chn x )
   LOOP               ( *chn x )
   SWAP DROP          ( x )
;

\ A mechanism to wait on multiple channel operations
: SELECT
   \ Placeholder: Actual implementation would involve complex
   \ logic to check readiness of multiple channels and handle the
   \ corresponding actions for the first ready channel
   ." Select called" CR
;

: YIELD
\ Yields control back to the coroutine scheduler
   \ Placeholder: Actual implementation would suspend the
   \ current coroutine and possibly switch to another waiting coroutine
   ." Yield called" CR
;

\ Wait for a coroutine to complete
: JOIN ( xt -- )
   \ Placeholder: Actual implementation would block current
   \ coroutine until the specified coroutine completes
   DROP
   ." Joining coroutine" CR
;

\ Gets the execution token of the current coroutine
: GO@ ( -- xt )
   \ Placeholder: Actual implementation would retrieve
   \ and return an execution token of the current coroutine
   ." Getting current coroutine" CR
   0 \ Placeholder return value
;

