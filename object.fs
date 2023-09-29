\ +===================================================================+
\  32bit Tagged Scheme Objects
\ +===================================================================+
\  This is a 32 bit tagged pointer system for Forth ipmlementing an
\  object system for scheme.
\
\ +-------------------------------------------------------------------+
\  Goals:
\ +-------------------------------------------------------------------+
\  1. #f must be a 32 bit word that is 0 and #t must be a -1 so that
\     branching works the same between wasm, forth, and scheme
\  2. It should be easy to determine a value's type from its 32 bit
\     word and the value in memory that it points to if it is a
\     dynamic object
\  3. Utilize the available space to avoid allocations and pointer
\     chasing, it should be very fast to get on the fast path 

\ +-------------------------------------------------------------------+
\  Constraints:
\ +-------------------------------------------------------------------+
\  1. Fixnums must be odd because #f cannot conflict with 0
\  2. Fixnums cannot be 31 bit because #t cannot conflict with -1
\  3. The first two bits of #t must be 11
\  4. We'll make 11 the tag for immediate values so that -1 naturally 
\     fits
\  5. If the first bit is 0 it is either a dynamic object or false
\     * false is a "dynamic object" that's ok I suaaose
\     * C null maps to false? probably ok?
\     * Other caveats?
\  6. If the first bit is 1 it is an immediate
\       i.  If the first two bits are 11 we already know it must be an
\           immediate
\       ii. If the first two bits are 01 we'll use that for fixnums
\  9. We have an extra bit to use for dynamic objects
\     We'll use that to distinguish a special first class object
\       i.  If the first two bits are 10 it is a pair
\       ii. If the first two bits are 00 it is another dynamic object
 
\ +-------------------------------------------------------------------+
\  Word mapping;                  
\ +-------------------------------------------------------------------+
\  The first two bits determine the type class of the object. one of 
\  1. #f
\  2. obj
\  3. pair
\  4. fixnum
\  5. imm
\
\  Immediates extend the type tag to the entire first byte and can
\  store up to 24 bits of immediate data so far we have
\  1. byte
\  2. character
\  3. nil
\  4. eof-object
\  5. #t

\ +-------------------------------------------------------------------+
\  bit layout
\ +-------------------------------------------------------------------+
\  tag   31     24 23     16 15     8  765432  10   scheme     -> forth
\        xxxxxxxx  xxxxxxxx  xxxxxxxx  tttttt  tt
\  00    00000000  00000000  00000000  000000  00   #f         -> 0
\        ^0
\  01    nnnnnnnn  nnnnnnnn  nnnnnnnn  nnnnnn  01   fixnum     -> n
\        ^i30
\  02    aaaaaaaa  aaaaaaaa  aaaaaaaa  aaaaaa  10   pair       -> addr
\        ^addr 
\  03    00000000  00000000  bbbbbbbb  000000  11   byte       -> b
\                            ^byte
\  04    aaaaaaaa  aaaaaaaa  aaaaaaaa  aaaaaa  00   object     -> addr
\        ^addr
\  ...   ...      ...      ...         ...          ...        -> ...
\  07    000nnnnn  nnnnnnnn  nnnnnnnn  000001  11   character  -> n
\           ^21 bit unicode scalar
\  ...   ...      ...      ...         ...          ...        -> ...
\  ff    xxxxxxxx  xxxxxxxx  xxxxxxxx  111111  11   singleton  -> x
\  ff    00000000  00000000  00000000  111111  11   nil        -> 255
\  ff    00000000  00000000  00000001  111111  11   eof-object -> 511
\  ff    11111111  11111111  11111111  111111  11   #t         -> -1


3 INVERT CONSTANT ADDR-MASK

: OBJ@     ( obj   -- x    )   OBJ> @ ;
: OBJ!     ( x obj --      )   OBJ> ! ;
: OBJ?     ( obj   -- flag )   1 AND 0= ;
: !OBJ>    ( obj   -- addr )   ADDR-MASK AND ;
: OBJ>     ( obj   -- addr )
   DUP OBJ? IF
      !OBJ>
   ELSE 
      ." Not an object "
      ABORT
   THEN 
;

: PAIR?    ( obj -- flag )   2 AND 2 = ;
: !PAIR>   ( obj -- addr )   ADDR-MASK AND ;
: PAIR>    ( obj -- addr )
   DUP PAIR? IF
      !PAIR>
   ELSE
      ." Not a pair "
      ABORT
   THEN ;
: >PAIR    ( addr -- obj )   2 OR ;

: FIXNUM?  ( obj -- flag )   1 AND 1 = ;
: !FIXNUM> ( obj -- n )      2 RSHIFT ;
: FIXNUM>  ( obj -- n )  
   DUP FIXNUM? IF
      !FIXNUM>
   ELSE
      ." Not a fixnum" 
      ABORT
   THEN
;
: >FIXNUM  ( n -- obj )      2 LSHIFT 1 OR ;

: IMM?     ( obj -- flag )   3 AND 3 = ;
: !IMM>    ( obj -- n )      ADDR-MASK AND ;
: IMM>     ( obj -- n )
   DUP IMM? IF
      !IMM>
   ELSE
      ." Not an immediate"
      ABORT
   THEN
;

\ +-------------------------------------------------------------------+
\  xxxxxx11     Immediate values                 
\ +-------------------------------------------------------------------+
\  8bit-tag   31     24 23     16 15     8  7654  32  10   Type       
\  hex  dec   xxxxxxxx  xxxxxxxx  tttt  11  11
\   03    3   00000000  00000000  00000000  0000  00  11   nil        
\   1f   31   000nnnnn  nnnnnnnn  nnnnnnnn  0011  11  11   character  
\   7f   47   00000000  00000000  bbbbbbbb  0111  11  11   byte       
\   ff  255   01111111  11111111  11111111  1111  11  11   eof-object 
\   ff  255   11111111  11111111  11111111  1111  11  11   #t         

255 CONSTANT IMM-TAG-MASK
8 CONSTANT IMM-TAG-LENGTH

3 CONSTANT BYTE-TAG
255 CONSTANT FULL-BYTE
8 CONSTANT BYTE-OFFSET

: BYTE?   ( obj -- flag )   IMM-TAG-MASK AND BYTE-TAG = ;
: !BYTE>  ( obj -- n )      BYTE-OFFSET RSHIFT ;
: !!>BYTE ( n -- byte )     BYTE-OFFSET LSHIFT BYTE-TAG OR ;
: !>BYTE  ( n -- byte )     FULL-BYTE AND !!>BYTE ;

: BYTE>   ( obj -- n )
   DUP BYTE? IF !BYTE> ELSE ." Not a byte" ABORT THEN
;

: >BYTE   ( n -- byte )
   DUP FULL-BYTE > IF
      ." Not a byte"
      ABORT
   ELSE
      !!>BYTE
   THEN 
;


7 CONSTANT CHARACTER-TAG
21 CONSTANT CHARACTER-LENGTH

: CHARACTER?   ( obj -- flag )   IMM-TAG-MASK AND CHARACTER-TAG = ;
: !CHARACTER>  ( obj -- n )      8 RSHIFT ;
: !!>CHARACTER ( n -- char )     8 LSHIFT CHARACTER-TAG OR ;
: !>CHARACTER  ( n -- char )     255 AND !!>CHARACTER ;

: CHARACTER>   ( obj -- n )
   DUP CHARACTER? IF
      !CHARACTER>
   ELSE 
      ." Not a character"
      ABORT
   THEN
;

: >CHARACTER   ( n -- char )
   DUP 255 > IF
      !!>CHARACTER
   ELSE 
      ." Not a character"
      ABORT
   THEN
;

255 CONSTANT SINGLETON-TAG

: SINGLETON?   ( obj -- flag )   IMM-TAG-MASK AND SINGLETON-TAG = ;

255 CONSTANT NIL
256 CONSTANT EOF-OBJECT
-1 CONSTANT #t


\ +-------------------------------------------------------------------+
\  Object Structure
\ +-------------------------------------------------------------------+
\  WORD0
\  31      24 23      16 15     8  76543210    Description
\  rrrrrrrrr  rrrrrrrrr  x-------  ttttttt0    Object Header
\  ^refcount             ^typeinfo ^typetag
\                                       ^gcmark  
\ +-------------------------------------------------------------------+
\  Legend
\  r -> 16 bit reference count
\       The maximum reference count is 2^16-1 = 65535
\       Once the reference count reaches this number it will no longer
\       be decremented and will need to be garbage collected
\  x -> type specific flag
\  m -> GC Mark Bit - All type tags will be even so we can smuggle our
\       mark bit in the least significant bit of the type tag
\  - -> Unused GC flags
\  t -> 8 bit Type Tag mask off thi mark bit and this matches other
\       type tags
\ +-------------------------------------------------------------------+
\  WORD1 Size of object in words 
\ +-------------------------------------------------------------------+
\  Bytes 8 ... n+8  Object specific data

65535
CONSTANT MAX-REFCOUNT
16
CONSTANT REFCOUNT-OFFSET
1 REFCOUNT-OFFSET LSHIFT
CONSTANT REFCOUNT1

: REFCOUNT ( obj -- )
   OBJ@ REFCOUNT-OFFSET RSHIFT
;

: RETAIN ( obj -- )
   REFCOUNT
   MAX-REFCOUNT < IF
      OBJ@ REFCOUNT1 + OBJ!
   THEN
;

: RELEASE ( obj -- )
   REFCOUNT 
   DUP 0= IF
      FREE
   ELSE MAX-REFCOUNT < IF
      OBJ@ REFCOUNT1 - OBJ!
   THEN THEN
;


\ +-------------------------------------------------------------------+
\  Object Types
\ +-------------------------------------------------------------------+
\  Reserved tags
\  76543 2 10
\  xxxxx x x0   hex   dec    type     Description         
\  xxxxx x x1   --1-----1--  imm      not a dynamic object
\  xxxxx x 10   --2-----2--  pair     imposed from above     
\  00000 0 00     0     0    #f       imposed from above
\  xxxxx 1 00   --4-----4--  number   if bit2 is set the tag is
\                                     reserved for the numeric tower
\ +-------------------------------------------------------------------+
\  00001 0 00     8     8    
\  00010 0 00    10    16    
\  00011 0 00    18    24    
\  00100 0 00    20    32    
\  00101 0 00    28    40    
\  00110 0 00    30    48    
\  00111 0 00    38    56    
\  01000 0 00    40    64    
\  01001 0 00    48    72    
\  01010 0 00    50    80    
\  01011 0 00    58    88    
\  01100 0 00    60    96    
\  01101 0 00    68   104    
\  01110 0 00    70   112    
\  01111 0 00    78   120    
\  10000 0 00    80   128    
\  10001 0 00    88   136    
\  10010 0 00    90   144    
\  10011 0 00    98   152    
\  10100 0 00   100   160    
\  10101 0 00   108   168    
\  10110 0 00   110   176    
\  10111 0 00   118   184    
\  11000 0 00   120   192    
\  11001 0 00   128   200    
\  11010 0 00   130   208    
\  11011 0 00   138   216    
\  11100 0 00   140   224    
\  11101 0 00   148   232    
\  11110 0 00   150   240    
\  11111 0 00   158   248   record 

2 CONSTANT PAIR-TAG
3 CELLS CONSTANT PAIR-SIZE
1 CELLS CONSTANT CAR-OFFSET
2 CELLS CONSTANT CDR-OFFSET

: CAR@ ( *pair -- *obj ) CAR-OFFSET + OBJ@ ;
: CDR@ ( *pair -- *obj ) CDR-OFFSET + OBJ@ ;
: CDR+CAR@ ( *pair -- *d *a ) CDR@ CAR@ ;

: CAR! ( *pair -- ) CAR-OFFSET + OBJ! ;
: CDR! ( *pair -- ) CDR-OFFSET + OBJ! ;

: CONS ( d a -- )
   \ Allocate the cells
   3 CELLS ALLOCATE       ( d a addr )
   \ Create the refcount and type tag header
   REFCOUNT1 PAIR-TAG +   ( d a addr header )
   \ Set the header
   OVER !                 ( d a addr )
   \ Write the car and cdr
   TUCK CAR!              ( d addr )
   TUCK CDR!              ( addr )
   \ Tag the address
   >PAIR                        
;

