\ +---------------------------------------------------------------------------+
\ Forth Words
\ +---------------------------------------------------------------------------+
\ ]                                                       Switches to compile state.
\ [CHAR]         ( -- char )                              Get the ASCII value of the next character in the input stream
\ [']            ( -- xt )                                Get the execution token of the next word
\ [                                                       Switches to interpretation state.
\ XOR            ( x 1 x2 -- x3 )                         Bitwise exclusive OR of x1 with x2
\ WORDS                                                   Display a list of words in the current vocabulary.
\ WORD           ( char -- addr )                         Parse a word from the input stream, using char as a delimiter
\ WITHIN         ( n  lo hi -- flag )                     True if lo <= n < hi
\ WHILE                                                   Conditional within a DO...LOOP or ?DO...LOOP.
\ VARIABLE       <name>                                   Create a variable in the dictionary
\ VALUE          <name>                                   Create a named constant
\ UNUSED         ( -- u )                                 Returns the number of unused bytes in the dictionary
\ UNTIL                                                   Terminate a BEGIN...UNTIL loop if top of stack is true.
\ UNLOOP                                                  in loops, e.g. to suaaort EXIT within a loop.
\ UM/MOD         ( u d n -- rem quot )                    Unsigned division, returning quotient and remainder
\ UM*            ( u 1 u2 -- ud )                         Unsigned multiplication
\ U>             ( u 1 u2 -- flag )                       True if u1 > u2
\ U<             ( u 1 u2 -- flag )                       True if u1 < u2
\ U.             ( u  -- )                                Display unsigned number
\ TYPE           ( a ddr u -- )                           Display a string
\ TUCK           ( x1 x2 -- x2 x1 x2 )                    Tucks x1 below x2
\ TRUE           ( -- flag )                              Pushes true ( usually -1) to the stack
\ TO                                                      Used to set VALUEs.
\ THEN                                                    Marks the end of an IF or ELSE clause.
\ SWAP           ( x1 x2 -- x2 x1 )                       Swaps top two stack items
\ STATE          ( -- addr )                              Address of the variable holding the interpreter's state (0 = interpreting, non-zero = compiling )
\ SPACES         ( n  -- )                                Print n spaces
\ SPACE          ( -- )                                   Print a space
\ SOURCE-ID      ( -- n )                                 Identifier for the input source
\ SOURCE         ( -- addr u )                            Address and length of the input buffer
\ SM/REM         ( d  n -- rem quot )                     Signed division, returning quotient and remainder
\ SIGN           char                                     Used in number formatting to deal with the sign
\ SCALL                                                   Used for system calls.
\ SAVE-INPUT     ( -- xn ... x1 n)                        Save the current input source specification
\ S>D            ( n  -- d )                              Convert single to double number
\ S\"                                                     Compile a string, but interpret its content in execute time
\ S"                                                      Compile a string
\ RSHIFT         ( x  u -- y )                            Bitwise right shift
\ ROT            ( x1 x2 x3 -- x2 x3 x1 )                 Rotate top three stack items
\ ROLL           ( x1 x2 ... xn n -- x2 ... xn x1 )       Rolls nth stack item to top
\ RESTORE-INPUT  ( x1 x2 ... xn -- flag  )                Restore the input source specification
\ REPEAT                                                  Marks the end of a BEGIN...WHILE...REPEAT loop.
\ REFILL         ( -- flag )                              Refill the input buffer
\ RECURSE                                                 Call the currently-defined word recursively.
\ R@             ( r : ... x -- x ) ( -- x )              Copy item from return stack
\ R>             ( r : ... x -- )   ( -- x )              Move item from return stack to data stack
\ QUIT                                                    Begin new interpreter session.
\ POSTPONE       <word>                                   Compile the compilation behavior of the following word
\ PICK           ( x1 x2 ... xn n -- x1 x2 ... xn xn-1 )  Copy nth stack item to top
\ PARSE-NAME     ( -- addr u )                            Parse a word from the input stream
\ PARSE          ( char -- addr u )                       Parse a sequence up to a character
\ PAD            ( -- addr )                              Temporary storage area
\ OVER           ( x1 x2 -- x1 x2 x1 )                    Copy second item to top
\ OR             ( x1 x2 -- x3 )                          Bitwise OR of x1 and x2
\ NIP            ( x1 x2 -- x2 )                          Remove second item from stack
\ NEGATE         ( n -- -n )                              Negates the number
\ MOVE           ( addr1 addr2 u -- )                     Move u bytes from addr1 to addr2
\ MOD            ( n1 n2 -- n3 )                          Remainder after division
\ MIN            ( n1 n2 -- n )                           Minimum of n1 and n2
\ MAX            ( n1 n2 -- n )                           Maximum of n1 and n2
\ MARKER         <name>                                   Create a point in the dictionary that can be used to undo subsequent definitions
\ M*             ( n1 n2 -- d )                           Multiply two numbers producing a double-result
\ LSHIFT         ( x  u -- y )                            Bitwise left shift
\ LOOP                                                    Increment loop index and branch if within loop limits.
\ LITERAL        ( Compile-time )                         Compile a number or address.
\ LEAVE                                                   Set loop index to end value, so LOOP exits immediately.
\ LATEST         ( -- addr )                              Address of the latest defined word
\ KEY            ( -- char )                              Get a character from input
\ J              ( -- n )                                 Loop index of outer loop in nested DO...LOOP structures
\ IS                                                      Assign behavior to a DEFERred word.
\ INVERT         ( x  -- y )                              Bitwise invert
\ IMMEDIATE                                               Mark the most recently defined word as immediate.
\ IF                                                      Conditional branch.
\ I              ( -- n )                                 Loop index in DO...LOOP structures
\ HOLDS          char                                     Used in number formatting
\ HOLD           char                                     Used in number formatting
\ HEX                                                     Set number conversion to hexadecimal.
\ HERE           ( -- addr )                              Current dictionary addr   
\ FM/MOD         ( d n -- rem quot )                      Division, returning quotient and remainder
\ FIND           ( addr -- xt flag )                      Find a word in the dictionary
\ FILL           ( addr u char -- )                       Fill memory with a character
\ FALSE          ( -- flag )                              Pushes false ( usually 0) to the stack
\ EXIT                                                    Return from the current word.
\ EXECUTE        ( xt -- ... )                            Execute the word with execution token xt
\ EVALUATE       ( addr u -- )                            Evaluate a Forth string
\ ERASE          ( addr u -- )                            Erase memory
\ ENVIRONMENT?   ( addr u -- addr u flag )                Query environment features
\ EMIT           ( char -- )                              Display a character
\ ELSE                                                    Alternative branch for IF.
\ DUP            ( x -- x x )                             Duplicate top stack item
\ DROP           ( x -- ... )                             Remove top stack item
\ DOES>                                                   Define run-time behavior for a word.
\ DO                                                      Start a loop.
\ DEPTH          ( -- n )                                 Number of items on the stack
\ DEFER@         ( addr -- xt )                           Fetch execution token from a deferred word
\ DEFER!         ( xt addr -- )                           Store execution token into a deferred word
\ DEFER          <name>                                   Create a deferred word
\ DECIMAL                                                 Set number conversion to decimal.
\ CREATE         <name>                                   Creates a new dictionary header
\ CR             ( -- )                                   Carriage Return, move to next line
\ COUNT          ( addr -- addr' u)                       G et length of counted string
\ CONSTANT       <name>                                   Create a named constant
\ COMPILE,       ( xt -- )                                Compile execution token
\ CHARS          ( n -- n' )                              Convert count in characters to count in address units, often a no-op
\ CHAR+          ( addr -- addr' )                        Add size of one character to address
\ CHAR           ( -- char )                              Fetch the ASCII value of the next word
\ CELLS          ( n -- n' )                              Convert count in cells to count in address units
\ CELL+          ( addr -- addr' )                        Add size of one cell to address
\ C@             ( addr -- char )                         Fetch character from address
\ C"                                                      Compile a string
\ C,             ( char -- )                              Store a character in the dictionary
\ C!             ( char addr -- )                         Store a character to address
\ BYE                                                     Exit the Forth system.
\ BUFFER:        ( u -- ) <name>                          Allocate a named buffer of u bytes
\ BL             ( -- char )                              ASCII value of a space
\ BEGIN                                                   Start of a control loop.
\ BASE           ( -- addr )                              Address of the current number conversion radix
\ AND            ( x 1 x2 -- x3 )                         Bitwise AND of x1 and x2
\ ALLOT          ( n  -- )                                Allocate n bytes in the dictionary
\ ALIGNED        ( addr -- addr' )                        Align address to cell size
\ ALIGN          ( -- )                                   Align HERE to cell size
\ ACTION-OF      <name> -- xt                             Execution token of action associated with a deferred word
\ ACCEPT         ( addr u1 -- u2 )                        Receive input into a buffer
\ ABS            ( -- n|u )                               Absolute value
\ ABORT"                                                  Compile-time Conditionally abort with message.
\ ABORT          ( -- )                                   Abort execution
\ @              ( a ddr -- x )                           Fetch cell from address
\ ?DUP           ( x -- ... x | ... x x )                 Duplicate if non-zero
\ >R             ( x  -- ) (r: ... -- x )                 Move item to return stack
\ >NUMBER        ( u d1 addr1 u1 -- ud2 addr2 u2 )        Convert string to number
\ >IN            ( -- addr )                              Address of input buffer addr   
\ >BODY          ( xt -- addr )                           Address of the data space for a word
\ >              ( n1 n2 -- flag )                        True if n1 > n2
\ =              ( x1 x2 -- flag )                        True if x1 equals x2
\ <#                                                      Begin number conversion.
\ <              ( n1 n2 -- flag )                        True if n1 < n2
\ ;                                                       End a colon definition.
\ :              <name>                                   Begin a colon definition.
\ 2SWAP          ( x1 x2 x3 x4 -- x3 x4 x1 x2 )           Swap two pairs of items
\ 2R>            ( R: ... x1 x2 -- ) -- x1 x2             Move two items from return stack to data stack
\ 2R@            ( R: ... x1 x2 -- x1 x2) -- x1 x2        Copy two items from return stack
\ 2OVER          ( x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2 )     Copy second pair to top
\ 2DUP           ( x1 x2 -- x1 x2 x1 x2 )                 Duplicate top pair of stack items
\ 2DROP          ( x1 x2 -- )                             Remove top two stack items
\ 2>R            ( x1 x2 -- ) (R: ... -- x1 x2)           Move two items to return stack
\ 2@             ( addr -- x1 x2 )                        Fetch two cells from address
\ 2/             ( x -- y )                               Arithmetic shift right
\ 2*             ( x -- y )                               Arithmetic shift left
\ 2!             ( x1 x2 addr -- )                        Store two cells to address
\ 1-             ( n -- n-1)                              Decrement
\ 1+             ( n -- n+1)                              Increment
\ 0>             ( n -- flag )                            True if n > 0
\ 0=             ( x -- flag )                            True if x equals 0
\ 0<>            ( x -- flag )                            True if x is not equal to 0
\ 0<             ( n -- flag )                            True if n < 0
\ <>             ( x1 x2 -- flag )                        True if x1 is not equal to x2
\ /MOD           ( n1 n2 -- rem quot )                    Division, returning quotient and remainder
\ /              ( n1 n2 -- quot )                        Division
\ .S                                                      Display stack without altering it
\ ."                                                      Compile a string to be displayed when executed
\ .              ( n  -- )                                Display a number
\ -              ( n1 n2 -- n3 )                          Subtraction
\ ,              ( x  -- )                                Store cell in the dictionary
\ +LOOP          ( n  -- )                                Increment loop index by n and branch if within loop limits
\ +!             ( n  addr -- )                           Add n to the cell at addr
\ */MOD          ( n1 n2 n3 -- rem quot )                 Multiply and divide
\ */             ( n1 n2 n3 -- n4 )                       Multiply two numbers and then divide
\ *              ( n1 n2 -- n3 )                          Multiplication
\  (                                                      Begin a comment, ends at next.
\ '              ( <name> -- xt )                         Get execution token of a word
\ #S                                                      Double number conversion loop.
\ #>                                                      Finish number conversion.
\ #                                                       Single number conversion.
\ .(                                                      Compile inline string to be displayed
\ ?DO            ( n1 n2 -- )                             Start a loop, skip if n1 equals n2
\ !              ( x  addr -- )                           Store x at addr
\ :NONAME                                                 Anonymous colon definition
\ +---------------------------------------------------------------------------+
