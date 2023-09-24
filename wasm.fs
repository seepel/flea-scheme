\ +====================================================================+
\  32 Bit WASM Assembler
\  Each word writes a single wasm instruction.
\ 
\  For example, given the following forth
\  ```forth
\  CODE DUP' ( n -- n n )
\    \ Put pointer to top of Forth stack (local_0) on the
\    \ Wasm operand stack (for use later)
\    [ 0 ] $LOCAL.GET
\ 
\    \ Load the number at the top of the Forth stack
\    \ (local_0 - 4) on the Wasm operand stack
\    [ 0 ] $LOCAL.GET
\    [ 4 ] $I32.CONST
\    $I32.SUB
\    $I32.LOAD
\ 
\    \ Store the number on the Wasm operand stack on
\    \ top of the Forth stack. The first operand (Forth
\    \ stack pointer) was put on the Wasm operand stack
\    \ at the beginning of this snippet
\    $I32.STORE
\ 
\    \ Increment the Forth top of stack pointer (local_0),
\    \ and leave it on the Wasm operand stack as return value
\    [ 0 ] $LOCAL.GET
\    [ 4 ] $I32.CONST
\    $I32.ADD
\  ;CODE
\  ```forth
\ 
\  A new word will be defined with the following WASM
\  ```wasm
\  (func $DUP' (param $tos i32) (result i32)
\    local.get $tos
\    local.get $tos
\    i32.const 4
\    i32.sub
\    i32.load
\    i32.store
\    local.get $tos
\    i32.const 4
\    i32.add
\  )
\  ```

\ +====================================================================+
\  Base Assembler words
\ +====================================================================+
\  These words are implemented by the wasm interpreter and write
\  webassembly instructions directly into the definition of the word
\  being compiled.
\ 
\   $U8,   (    u8 -- )  Writes an  8 bit unsigned integer
\   $S8,   (    s8 -- )  Writes an  8 bit signed integer
\   $U16,  (   u16 -- )  Writes a 16 bit unsigned integer
\   $S16,  (   s16 -- )  Writes a 16 bit signed integer
\   $U32,  (   u32 -- )  Writes a 32 bit unsigned integer
\   $S32,  (   s32 -- )  Writes a 32 bit signed integer
\   $U64,  (  2u32 -- )  Writes a 64 bit unsigned integer
\   $S64,  (  2s32 -- )  Writes a 64 bit signed integer
\   $F32,  (     f -- )  Writes a 32 bit FLOAT
\   $F64,  (     d -- )  Writes a 64 bit DOUBLE
\   $U128, ( 4u128 -- )  Writes a 128 bit unsigned integer
\   $S128, ( 4s128 -- )  Writes a 128 bit signed integer
\   $BT,   (     ? -- )  Writes a  block type
\   $TYP,  (     ? -- )  Writes a  value type
\   $MEM,  (     ? -- )  Writes a  memory argument
\   $LCL,  (     ? -- )  Writes a  local argument
\   $GBL,  (     ? -- )  Writes a  global argument
\   $TBL,  (     ? -- )  Writes a  table argument
\   $REF,  (     ? -- )  Writes a  reference type
\   $FNT,  (     ? -- )  Writes a  function type

\ There is only one memory and this is it
: @MEM ( -- ) 2 U, 0 U, ; IMMEDIATE

\ +====================================================================+
\  Instruction Formats
\ +====================================================================+
\  Webassembly instructions can be one to three bytes long.

: INST,  ( u -- )        $U8,           ; IMMEDIATE
: 2INST, ( u1 u2 -- )    $U8, $U8,      ; IMMEDIATE
: 3INST, ( u1 u2 u3 -- ) $U8, $U8, $U8, ; IMMEDIATE

\ +====================================================================+
\  Basic Instructions
\ +====================================================================+

\ 00 -> 0F
: $UNREACHABLE         ( 00 )   0 INST,        ; IMMEDIATE
: $NOP                 ( 01 )   1 INST,        ; IMMEDIATE
: $BLOCK               ( 02 )   2 INST, $BT,   ; IMMEDIATE
: $LOOP                ( 03 )   3 INST,        ; IMMEDIATE
: $IF                  ( 04 )   4 INST,        ; IMMEDIATE
: $ELSE                ( 05 )   5 INST,        ; IMMEDIATE
\ UNASSIGNED           ( 06 )
\ UNASSIGNED           ( 07 )
\ UNASSIGNED           ( 08 )
\ UNASSIGNED           ( 09 )
\ UNASSIGNED           ( 0A )
: $END                 ( 0B )  11 INST,        ; IMMEDIATE
: $BR                  ( 0C )  12 INST,        ; IMMEDIATE
: $BR_IF               ( 0D )  13 INST,        ; IMMEDIATE
: $BR_TABLE            ( 0E )  14 INST,        ; IMMEDIATE
: $RETURN              ( 0F )  15 INST,        ; IMMEDIATE

\ 10 -> 1F
: $CALL                ( 10 )  16 INST,        ; IMMEDIATE
: $CALL_IND            ( 11 )  17 INST,        ; IMMEDIATE
\ UNASSIGNED           ( 12 )
\ UNASSIGNED           ( 13 )
\ UNASSIGNED           ( 14 )
\ UNASSIGNED           ( 15 )
\ UNASSIGNED           ( 16 )
\ UNASSIGNED           ( 17 )
\ UNASSIGNED           ( 18 )
\ UNASSIGNED           ( 19 )
: $DROP                ( 1A )  26 INST,        ; IMMEDIATE
: $SELECT              ( 1B )  27 INST,        ; IMMEDIATE
: $SELECTT             ( 1C )  28 INST,        ; IMMEDIATE

\ 20 -> 2F
: $LOCAL.GET           ( 20 )  32 INST, $U32,  ; IMMEDIATE
: $LOCAL.SET           ( 21 )  33 INST, $U32,  ; IMMEDIATE
: $LOCAL.TEE           ( 22 )  34 INST, $U32,  ; IMMEDIATE
: $GLOBAL.GET          ( 23 )  35 INST, $U32,  ; IMMEDIATE
: $GLOBAL.SET          ( 24 )  36 INST, $U32,  ; IMMEDIATE
: $TABLE.GET           ( 25 )  37 INST, $U32,  ; IMMEDIATE
: $TABLE.SET           ( 26 )  38 INST, $U32,  ; IMMEDIATE
\ UNASSIGNED           ( 27 )
: $I32.LOAD            ( 28 )  40 INST, @MEM,  ; IMMEDIATE
: $I64.LOAD            ( 29 )  41 INST, @MEM,  ; IMMEDIATE
: $F32.LOAD            ( 2A )  42 INST, @MEM,  ; IMMEDIATE
: $F64.LOAD            ( 2B )  43 INST, @MEM,  ; IMMEDIATE
: $I32.LOAD8_S         ( 2C )  44 INST, @MEM,  ; IMMEDIATE
: $I32.LOAD8_U         ( 2D )  45 INST, @MEM,  ; IMMEDIATE
: $I32.LOAD16_S        ( 2E )  46 INST, @MEM,  ; IMMEDIATE
: $I32.LOAD16_U        ( 2F )  47 INST, @MEM,  ; IMMEDIATE

\ 30 -> 3F
: $I64.LOAD8_S         ( 30 )  48 INST, @MEM,  ; IMMEDIATE
: $I64.LOAD8_U         ( 31 )  49 INST, @MEM,  ; IMMEDIATE
: $I64.LOAD16_S        ( 32 )  50 INST, @MEM,  ; IMMEDIATE
: $I64.LOAD16_U        ( 33 )  51 INST, @MEM,  ; IMMEDIATE
: $I64.LOAD32_S        ( 34 )  52 INST, @MEM,  ; IMMEDIATE
: $I64.LOAD32_U        ( 35 )  53 INST, @MEM,  ; IMMEDIATE
: $I32.STORE           ( 36 )  54 INST, @MEM,  ; IMMEDIATE
: $I64.STORE           ( 37 )  55 INST, @MEM,  ; IMMEDIATE
: $F32.STORE           ( 38 )  56 INST, @MEM,  ; IMMEDIATE
: $F64.STORE           ( 39 )  57 INST, @MEM,  ; IMMEDIATE
: $I32.STORE8          ( 3A )  58 INST, @MEM,  ; IMMEDIATE
: $I32.STORE16         ( 3B )  59 INST, @MEM,  ; IMMEDIATE
: $I64.STORE8          ( 3C )  60 INST, @MEM,  ; IMMEDIATE
: $I64.STORE16         ( 3D )  61 INST, @MEM,  ; IMMEDIATE
: $I64.STORE32         ( 3E )  62 INST, @MEM,  ; IMMEDIATE
: $MEMORY.SIZE         ( 3F )  63 INST, $U32,  ; IMMEDIATE

\ 40 -> 4F
: $MEMORY.GROW         ( 40 )  64 INST, $U32,  ; IMMEDIATE
: $I32.CONST           ( 41 )  65 INST, $S32,  ; IMMEDIATE
: $I64.CONST           ( 42 )  66 INST, $S64,  ; IMMEDIATE
: $F32.CONST           ( 43 )  67 INST, $FLT,  ; IMMEDIATE
: $F64.CONST           ( 44 )  68 INST, $DBL,  ; IMMEDIATE
: $I32.EQZ             ( 45 )  69 INST,        ; IMMEDIATE
: $I32.EQ              ( 46 )  70 INST,        ; IMMEDIATE
: $I32.NE              ( 47 )  71 INST,        ; IMMEDIATE
: $I32.LT_S            ( 48 )  72 INST,        ; IMMEDIATE
: $I32.LT_U            ( 49 )  73 INST,        ; IMMEDIATE
: $I32.GT_S            ( 4A )  74 INST,        ; IMMEDIATE
: $I32.GT_U            ( 4B )  75 INST,        ; IMMEDIATE
: $I32.LE_S            ( 4C )  76 INST,        ; IMMEDIATE
: $I32.LE_U            ( 4D )  77 INST,        ; IMMEDIATE
: $I32.GE_S            ( 4E )  78 INST,        ; IMMEDIATE
: $I32.GE_U            ( 4F )  79 INST,        ; IMMEDIATE

\ 50 -> 5F
: $I64.EQZ             ( 50 )  80 INST,        ; IMMEDIATE
: $I64.EQ              ( 51 )  81 INST,        ; IMMEDIATE
: $I64.NE              ( 52 )  82 INST,        ; IMMEDIATE
: $I64.LT_S            ( 53 )  83 INST,        ; IMMEDIATE
: $I64.LT_U            ( 54 )  84 INST,        ; IMMEDIATE
: $I64.GT_S            ( 55 )  85 INST,        ; IMMEDIATE
: $I64.GT_U            ( 56 )  86 INST,        ; IMMEDIATE
: $I64.LE_S            ( 57 )  87 INST,        ; IMMEDIATE
: $I64.LE_U            ( 58 )  88 INST,        ; IMMEDIATE
: $I64.GE_S            ( 59 )  89 INST,        ; IMMEDIATE
: $I64.GE_U            ( 5A )  90 INST,        ; IMMEDIATE
: $F32.EQ              ( 5B )  91 INST,        ; IMMEDIATE
: $F32.NE              ( 5C )  92 INST,        ; IMMEDIATE
: $F32.LT              ( 5D )  93 INST,        ; IMMEDIATE
: $F32.GT              ( 5E )  94 INST,        ; IMMEDIATE
: $F32.LE              ( 5F )  95 INST,        ; IMMEDIATE

\ 60 -> 6F
: $F32.GE              ( 60 )  96 INST,        ; IMMEDIATE
: $F64.EQ              ( 61 )  97 INST,        ; IMMEDIATE
: $F64.NE              ( 62 )  98 INST,        ; IMMEDIATE
: $F64.LT              ( 63 )  99 INST,        ; IMMEDIATE
: $F64.GT              ( 64 ) 100 INST,        ; IMMEDIATE
: $F64.LE              ( 65 ) 101 INST,        ; IMMEDIATE
: $F64.GE              ( 66 ) 102 INST,        ; IMMEDIATE
: $I32.CLZ             ( 67 ) 103 INST,        ; IMMEDIATE
: $I32.CTZ             ( 68 ) 104 INST,        ; IMMEDIATE
: $I32.POPCNT          ( 69 ) 105 INST,        ; IMMEDIATE
: $I32.ADD             ( 6A ) 106 INST,        ; IMMEDIATE
: $I32.SUB             ( 6B ) 107 INST,        ; IMMEDIATE
: $I32.MUL             ( 6C ) 108 INST,        ; IMMEDIATE
: $I32.DIV_S           ( 6D ) 109 INST,        ; IMMEDIATE
: $I32.DIV_U           ( 6E ) 110 INST,        ; IMMEDIATE
: $I32.REM_S           ( 6F ) 111 INST,        ; IMMEDIATE

\ 70 -> 7F
: $I32.REM_U           ( 70 ) 112 INST,        ; IMMEDIATE
: $I32.AND             ( 71 ) 113 INST,        ; IMMEDIATE
: $I32.OR              ( 72 ) 114 INST,        ; IMMEDIATE
: $I32.XOR             ( 73 ) 115 INST,        ; IMMEDIATE
: $I32.SHL             ( 74 ) 116 INST,        ; IMMEDIATE
: $I32.SHR_S           ( 75 ) 117 INST,        ; IMMEDIATE
: $I32.SHR_U           ( 76 ) 118 INST,        ; IMMEDIATE
: $I32.ROTL            ( 77 ) 119 INST,        ; IMMEDIATE
: $I32.ROTR            ( 78 ) 120 INST,        ; IMMEDIATE
: $I64.CLZ             ( 79 ) 121 INST,        ; IMMEDIATE
: $I64.CTZ             ( 7A ) 122 INST,        ; IMMEDIATE
: $I64.POPCNT          ( 7B ) 123 INST,        ; IMMEDIATE
: $I64.ADD             ( 7C ) 124 INST,        ; IMMEDIATE
: $I64.SUB             ( 7D ) 125 INST,        ; IMMEDIATE
: $I64.MUL             ( 7E ) 126 INST,        ; IMMEDIATE
: $I64.DIV_S           ( 7F ) 127 INST,        ; IMMEDIATE

\ 80 -> 8F
: $I64.DIV_U           ( 80 ) 128 INST,        ; IMMEDIATE
: $I64.REM_S           ( 81 ) 129 INST,        ; IMMEDIATE
: $I64.REM_U           ( 82 ) 130 INST,        ; IMMEDIATE
: $I64.AND             ( 83 ) 131 INST,        ; IMMEDIATE
: $I64.OR              ( 84 ) 132 INST,        ; IMMEDIATE
: $I64.XOR             ( 85 ) 133 INST,        ; IMMEDIATE
: $I64.SHL             ( 86 ) 134 INST,        ; IMMEDIATE
: $I64.SHR_S           ( 87 ) 135 INST,        ; IMMEDIATE
: $I64.SHR_U           ( 88 ) 136 INST,        ; IMMEDIATE
: $I64.ROTL            ( 89 ) 137 INST,        ; IMMEDIATE
: $I64.ROTR            ( 8A ) 138 INST,        ; IMMEDIATE
: $F32.ABS             ( 8B ) 139 INST,        ; IMMEDIATE
: $F32.NEG             ( 8C ) 140 INST,        ; IMMEDIATE
: $F32.CEIL            ( 8D ) 141 INST,        ; IMMEDIATE
: $F32.FLOOR           ( 8E ) 142 INST,        ; IMMEDIATE
: $F32.TRUNC           ( 8F ) 143 INST,        ; IMMEDIATE

\ 90 -> 9F
: $F32.NEAREST         ( 90 ) 144 INST,        ; IMMEDIATE
: $F32.SQRT            ( 91 ) 145 INST,        ; IMMEDIATE
: $F32.ADD             ( 92 ) 146 INST,        ; IMMEDIATE
: $F32.SUB             ( 93 ) 147 INST,        ; IMMEDIATE
: $F32.MUL             ( 94 ) 148 INST,        ; IMMEDIATE
: $F32.DIV             ( 95 ) 149 INST,        ; IMMEDIATE
: $F32.MIN             ( 96 ) 150 INST,        ; IMMEDIATE
: $F32.MAX             ( 97 ) 151 INST,        ; IMMEDIATE
: $F32.COPYSIGN        ( 98 ) 152 INST,        ; IMMEDIATE
: $F64.ABS             ( 99 ) 153 INST,        ; IMMEDIATE
: $F64.NEG             ( 9A ) 154 INST,        ; IMMEDIATE
: $F64.CEIL            ( 9B ) 155 INST,        ; IMMEDIATE
: $F64.FLOOR           ( 9C ) 156 INST,        ; IMMEDIATE
: $F64.TRUNC           ( 9D ) 157 INST,        ; IMMEDIATE
: $F64.NEAREST         ( 9E ) 158 INST,        ; IMMEDIATE
: $F64.SQRT            ( 9F ) 159 INST,        ; IMMEDIATE

\ A0 -> AF
: $F64.ADD             ( A0 ) 160 INST,        ; IMMEDIATE
: $F64.SUB             ( A1 ) 161 INST,        ; IMMEDIATE
: $F64.MUL             ( A2 ) 162 INST,        ; IMMEDIATE
: $F64.DIV             ( A3 ) 163 INST,        ; IMMEDIATE
: $F64.MIN             ( A4 ) 164 INST,        ; IMMEDIATE
: $F64.MAX             ( A5 ) 165 INST,        ; IMMEDIATE
: $F64.COPYSIGN        ( A6 ) 166 INST,        ; IMMEDIATE
: $I32.WRAP_I64        ( A7 ) 167 INST,        ; IMMEDIATE
: $I32.TRUNC_F32_S     ( A8 ) 168 INST,        ; IMMEDIATE
: $I32.TRUNC_F32_U     ( A9 ) 169 INST,        ; IMMEDIATE
: $I32.TRUNC_F64_S     ( AA ) 170 INST,        ; IMMEDIATE
: $I32.TRUNC_F64_U     ( AB ) 171 INST,        ; IMMEDIATE
: $I64.EXTEND_I32_S    ( AC ) 172 INST,        ; IMMEDIATE
: $I64.EXTEND_I32_U    ( AD ) 173 INST,        ; IMMEDIATE
: $I64.TRUNC_F32_S     ( AE ) 174 INST,        ; IMMEDIATE
: $I64.TRUNC_F32_U     ( AF ) 175 INST,        ; IMMEDIATE

\ B0 -> BF
: $I64.TRUNC_F64_S     ( B0 ) 176 INST,        ; IMMEDIATE
: $I64.TRUNC_F64_U     ( B1 ) 177 INST,        ; IMMEDIATE
: $F32.CONVERT_I32_S   ( B2 ) 178 INST,        ; IMMEDIATE
: $F32.CONVERT_I32_U   ( B3 ) 179 INST,        ; IMMEDIATE
: $F32.CONVERT_I64_S   ( B4 ) 180 INST,        ; IMMEDIATE
: $F32.CONVERT_I64_U   ( B5 ) 181 INST,        ; IMMEDIATE
: $F32.DEMOTE_F64      ( B6 ) 182 INST,        ; IMMEDIATE
: $F64.CONVERT_I32_S   ( B7 ) 183 INST,        ; IMMEDIATE
: $F64.CONVERT_I32_U   ( B8 ) 184 INST,        ; IMMEDIATE
: $F64.CONVERT_I64_S   ( B9 ) 185 INST,        ; IMMEDIATE
: $F64.CONVERT_I64_U   ( BA ) 186 INST,        ; IMMEDIATE
: $F64.PROMOTE_F32     ( BB ) 187 INST,        ; IMMEDIATE
: $I32.REINTERPRET_F32 ( BC ) 188 INST,        ; IMMEDIATE
: $I64.REINTERPRET_F64 ( BD ) 189 INST,        ; IMMEDIATE
: $F32.REINTERPRET_I32 ( BE ) 190 INST,        ; IMMEDIATE
: $F64.REINTERPRET_I64 ( BF ) 191 INST,        ; IMMEDIATE

\ C0 -> CF
: $I32.EXTEND8_S       ( C0 ) 192 INST,        ; IMMEDIATE
: $I32.EXTEND16_S      ( C1 ) 193 INST,        ; IMMEDIATE
: $I64.EXTEND8_S       ( C2 ) 194 INST,        ; IMMEDIATE
: $I64.EXTEND16_S      ( C3 ) 195 INST,        ; IMMEDIATE
: $I64.EXTEND32_S      ( C4 ) 196 INST,        ; IMMEDIATE

\ DO -> DF
: $REF.NULL            ( D0 ) 208 INST, $U32,  ; IMMEDIATE
: $REF.IS_NULL         ( D1 ) 209 INST,        ; IMMEDIATE
: $REF.FUNC            ( D2 ) 210 INST, $U32,  ; IMMEDIATE

\ EO -> EF
\ UNASSIGNED           224 -> 239 ( E0 -> EF )

\ F0 -> FB
\ UNASSIGNED           240 -> 251 ( F0 -> FB )


\ +====================================================================+
\  Extended Intsructions
\ +====================================================================+

\ FC XX...
: $I32.TRUNC_SAT_F32_S            ( 00 )     0 252 2INST,             ; IMMEDIATE
: $I32.TRUNC_SAT_F32_U            ( 01 )     1 252 2INST,             ; IMMEDIATE
: $I32.TRUNC_SAT_F64_S            ( 02 )     2 252 2INST,             ; IMMEDIATE
: $I32.TRUNC_SAT_F64_U            ( 03 )     3 252 2INST,             ; IMMEDIATE
: $I64.TRUNC_SAT_F32_S            ( 04 )     4 252 2INST,             ; IMMEDIATE
: $I64.TRUNC_SAT_F32_U            ( 05 )     5 252 2INST,             ; IMMEDIATE
: $I64.TRUNC_SAT_F64_S            ( 06 )     6 252 2INST,             ; IMMEDIATE
: $I64.TRUNC_SAT_F64_U            ( 07 )     7 252 2INST,             ; IMMEDIATE
: $MEMORY.INIT                    ( 08 )     8 252 2INST,  $S,        ; IMMEDIATE
: $DATA.DROP                      ( 09 )     9 252 2INST,  $S,        ; IMMEDIATE
: $MEMORY.COPY                    ( 0A )    10 252 2INST,  $S,        ; IMMEDIATE
: $MEMORY.FILL                    ( 0B )    11 252 2INST,             ; IMMEDIATE
: $TABLE.INIT                     ( 0C )    12 252 2INST,  $S, $S,    ; IMMEDIATE
: $ELEM.DROP                      ( 0D )    13 252 2INST,  $S,        ; IMMEDIATE
: $TABLE.COPY                     ( 0E )    14 252 2INST,  $S, $S,    ; IMMEDIATE
: $TABLE.GROW                     ( 0F )    15 252 2INST,  $S,        ; IMMEDIATE
: $TABLE.SIZE                     ( 10 )    16 252 2INST,  $S,        ; IMMEDIATE
: $TABLE.FILL                     ( 11 )    17 252 2INST,  $S,        ; IMMEDIATE

\ FD 00 -> FD 0F -----
: $V128.LOAD                      ( 00 )     0 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD8x8_S                 ( 01 )     1 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD8x8_U                 ( 02 )     2 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD16x4_S                ( 03 )     3 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD16x4_U                ( 04 )     4 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD32x2_S                ( 05 )     5 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD32x2_U                ( 06 )     6 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD8_SPLAT               ( 07 )     7 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD16_SPLAT              ( 08 )     8 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD32_SPLAT              ( 09 )     9 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD64_SPLAT              ( 0A )    10 253 2INST, @MEM,       ; IMMEDIATE
: $V128.STORE                     ( 0B )    11 253 2INST, @MEM,       ; IMMEDIATE
: $V128.CONST                     ( 0C )    12 253 2INST, $S128,      ; IMMEDIATE
: $I8x16.SHUFFLE                  ( 0D )    13 253 2INST, $S128,      ; IMMEDIATE
: $I8x16.SWIZZLE                  ( 0E )    14 253 2INST, $S128,      ; IMMEDIATE
: $I8x16.SPLAT                    ( 0F )    15 253 2INST,             ; IMMEDIATE

\ FD 10 -> FD 1F
: $I16x8.SPLAT                    ( 10 )    16 253 2INST,             ; IMMEDIATE
: $I32x4.SPLAT                    ( 11 )    17 253 2INST,             ; IMMEDIATE
: $I64x2.SPLAT                    ( 12 )    18 253 2INST,             ; IMMEDIATE
: $F32x4.SPLAT                    ( 13 )    19 253 2INST,             ; IMMEDIATE
: $F64x2.SPLAT                    ( 14 )    20 253 2INST,             ; IMMEDIATE
: $I8x16.EXTRACT_LANE_S           ( 15 )    21 253 2INST,             ; IMMEDIATE
: $I8x16.EXTRACT_LANE_U           ( 16 )    22 253 2INST, $S128,      ; IMMEDIATE
: $I8x16.REPLACE_LANE             ( 17 )    23 253 2INST, $S128,      ; IMMEDIATE
: $I16x8.EXTRACT_LANE_S           ( 18 )    24 253 2INST, $S128,      ; IMMEDIATE
: $I16x8.EXTRACT_LANE_U           ( 19 )    25 253 2INST, $S128,      ; IMMEDIATE
: $I16x8.REPLACE_LANE             ( 1A )    26 253 2INST, $S128,      ; IMMEDIATE
: $I32x4.EXTRACT_LANE             ( 1B )    27 253 2INST, $S128,      ; IMMEDIATE
: $I32x4.REPLACE_LANE             ( 1C )    28 253 2INST, $S128,      ; IMMEDIATE
: $I64x2.EXTRACT_LANE             ( 1D )    29 253 2INST, $S128,      ; IMMEDIATE
: $I64x2.REPLACE_LANE             ( 1E )    30 253 2INST, $S128,      ; IMMEDIATE
: $F32x4.EXTRACT_LANE             ( 1F )    31 253 2INST, $S128,      ; IMMEDIATE

\ FD 20 -> FD 2F
: $F32x4.REPLACE_LANE             ( 20 )    32 253 2INST, @MEM, $S128 ; IMMEDIATE
: $F64x2.EXTRACT_LANE             ( 21 )    33 253 2INST, @MEM, $S128 ; IMMEDIATE
: $F64x2.REPLACE_LANE             ( 22 )    34 253 2INST, @MEM, $S128 ; IMMEDIATE
: $I8x16.EQ                       ( 23 )    35 253 2INST,             ; IMMEDIATE
: $I8x16.NE                       ( 24 )    36 253 2INST,             ; IMMEDIATE
: $I8x16.LT_S                     ( 25 )    37 253 2INST,             ; IMMEDIATE
: $I8x16.LT_U                     ( 26 )    38 253 2INST,             ; IMMEDIATE
: $I8x16.GT_S                     ( 27 )    39 253 2INST,             ; IMMEDIATE
: $I8x16.GT_U                     ( 28 )    40 253 2INST,             ; IMMEDIATE
: $I8x16.LE_S                     ( 29 )    41 253 2INST,             ; IMMEDIATE
: $I8x16.LE_U                     ( 2A )    42 253 2INST,             ; IMMEDIATE
: $I8x16.GE_S                     ( 2B )    43 253 2INST,             ; IMMEDIATE
: $I8x16.GE_U                     ( 2C )    44 253 2INST,             ; IMMEDIATE
: $I16x8.EQ                       ( 2D )    45 253 2INST,             ; IMMEDIATE
: $I16x8.NE                       ( 2E )    46 253 2INST,             ; IMMEDIATE
: $I16x8.LT_S                     ( 2F )    47 253 2INST,             ; IMMEDIATE

\ FD 30 -> FD 3F
: $I16x8.LT_U                     ( 30 )    48 253 2INST,             ; IMMEDIATE
: $I16x8.GT_S                     ( 31 )    49 253 2INST,             ; IMMEDIATE
: $I16x8.GT_U                     ( 32 )    50 253 2INST,             ; IMMEDIATE
: $I16x8.LE_S                     ( 33 )    51 253 2INST,             ; IMMEDIATE
: $I16x8.LE_U                     ( 34 )    52 253 2INST,             ; IMMEDIATE
: $I16x8.GE_S                     ( 35 )    53 253 2INST,             ; IMMEDIATE
: $I16x8.GE_U                     ( 36 )    54 253 2INST,             ; IMMEDIATE
: $I32x4.EQ                       ( 37 )    55 253 2INST,             ; IMMEDIATE
: $I32x4.NE                       ( 38 )    56 253 2INST,             ; IMMEDIATE
: $I32x4.LT_S                     ( 39 )    57 253 2INST,             ; IMMEDIATE
: $I32x4.LT_U                     ( 3A )    58 253 2INST,             ; IMMEDIATE
: $I32x4.GT_S                     ( 3B )    59 253 2INST,             ; IMMEDIATE
: $I32x4.GT_U                     ( 3C )    60 253 2INST,             ; IMMEDIATE
: $I32x4.LE_S                     ( 3D )    61 253 2INST,             ; IMMEDIATE
: $I32x4.LE_U                     ( 3E )    62 253 2INST,             ; IMMEDIATE
: $I32x4.GE_S                     ( 3F )    63 253 2INST,             ; IMMEDIATE

\ FD 40 -> FD 4F
: $I32x4.GE_U                     ( 40 )   64 253 2INST,              ; IMMEDIATE
: $F32x4.EQ                       ( 41 )   65 253 2INST,              ; IMMEDIATE
: $F32x4.NE                       ( 42 )   66 253 2INST,              ; IMMEDIATE
: $F32x4.LT                       ( 43 )   67 253 2INST,              ; IMMEDIATE
: $F32x4.GT                       ( 44 )   68 253 2INST,              ; IMMEDIATE
: $F32x4.LE                       ( 45 )   69 253 2INST,              ; IMMEDIATE
: $F32x4.GE                       ( 46 )   70 253 2INST,              ; IMMEDIATE
: $F64x2.EQ                       ( 47 )   71 253 2INST,              ; IMMEDIATE
: $F64x2.NE                       ( 48 )   72 253 2INST,              ; IMMEDIATE
: $F64x2.LT                       ( 49 )   73 253 2INST,              ; IMMEDIATE
: $F64x2.GT                       ( 4A )   74 253 2INST,              ; IMMEDIATE
: $F64x2.LE                       ( 4B )   75 253 2INST,              ; IMMEDIATE
: $F64x2.GE                       ( 4C )   76 253 2INST,              ; IMMEDIATE
: $V128.NOT                       ( 4D )   77 253 2INST,              ; IMMEDIATE
: $V128.AND                       ( 4E )   78 253 2INST,              ; IMMEDIATE
: $V128.ANDNOT                    ( 4F )   79 253 2INST,              ; IMMEDIATE

\ FD 50 -> FD 5F
: $V128.OR                        ( 50 )    80 253 2INST,             ; IMMEDIATE
: $V128.XOR                       ( 51 )    81 253 2INST,             ; IMMEDIATE
: $V128.BITSELECT                 ( 52 )    82 253 2INST,             ; IMMEDIATE
: $V128.ANY_TRUE                  ( 53 )    83 253 2INST,             ; IMMEDIATE
: $V128.LOAD8_LANE                ( 54 )    84 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD16_LANE               ( 55 )    85 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD32_LANE               ( 56 )    86 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD64_LANE               ( 57 )    87 253 2INST, @MEM,       ; IMMEDIATE
: $V128.STORE8_LANE               ( 58 )    88 253 2INST, @MEM,       ; IMMEDIATE
: $V128.STORE16_LANE              ( 59 )    89 253 2INST, @MEM,       ; IMMEDIATE
: $V128.STORE32_LANE              ( 5A )    90 253 2INST, @MEM,       ; IMMEDIATE
: $V128.STORE64_LANE              ( 5B )    91 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD32_ZERO               ( 5C )    92 253 2INST, @MEM,       ; IMMEDIATE
: $V128.LOAD64_ZERO               ( 5D )    93 253 2INST, @MEM,       ; IMMEDIATE
: $F32x4.DEMOTE_F64x2_ZERO        ( 5E )    94 253 2INST,             ; IMMEDIATE
: $F64x2.PROMOTE_LOW_F32x4        ( 5F )    95 253 2INST,             ; IMMEDIATE

\ FD 60 -> FD 6F
: $I8x16.ABS                      ( 60 )    96 253 2INST,             ; IMMEDIATE
: $I8x16.NEG                      ( 61 )    97 253 2INST,             ; IMMEDIATE
: $I8x16.POPCNT                   ( 62 )    98 253 2INST,             ; IMMEDIATE
: $I8x16.ALL_TRUE                 ( 63 )    99 253 2INST,             ; IMMEDIATE
: $I8x16.BITMASK                  ( 64 )   100 253 2INST,             ; IMMEDIATE
: $I8x16.NARROW_I16x8_S           ( 65 )   101 253 2INST,             ; IMMEDIATE
: $I8x16.NARROW_I16x8_U           ( 66 )   102 253 2INST,             ; IMMEDIATE
: $F32x4.CEIL                     ( 67 )   103 253 2INST,             ; IMMEDIATE
: $F32x4.FLOOR                    ( 68 )   104 253 2INST,             ; IMMEDIATE
: $F32x4.TRUNC                    ( 69 )   105 253 2INST,             ; IMMEDIATE
: $F32x4.NEAREST                  ( 6A )   106 253 2INST,             ; IMMEDIATE
: $I8x16.SHL                      ( 6B )   107 253 2INST,             ; IMMEDIATE
: $I8x16.SHR_S                    ( 6C )   108 253 2INST,             ; IMMEDIATE
: $I8x16.SHR_U                    ( 6D )   109 253 2INST,             ; IMMEDIATE
: $I8x16.ADD                      ( 6E )   110 253 2INST,             ; IMMEDIATE
: $I8x16.ADD_SAT_S                ( 6F )   111 253 2INST,             ; IMMEDIATE

\ FD 70 -> FD 7F
: $I8x16.ADD_SAT_U                ( 70 )   112 253 2INST,             ; IMMEDIATE
: $I8x16.SUB                      ( 71 )   113 253 2INST,             ; IMMEDIATE
: $I8x16.SUB_SAT_S                ( 72 )   114 253 2INST,             ; IMMEDIATE
: $I8x16.SUB_SAT_U                ( 73 )   115 253 2INST,             ; IMMEDIATE
: $F64x2.CEIL                     ( 74 )   116 253 2INST,             ; IMMEDIATE
: $F64x2.FLOOR                    ( 75 )   117 253 2INST,             ; IMMEDIATE
: $i8x16.MIN_S                    ( 76 )   118 253 2INST,             ; IMMEDIATE
: $i8x16.MIN_U                    ( 77 )   119 253 2INST,             ; IMMEDIATE
: $i8x16.MAX_S                    ( 78 )   120 253 2INST,             ; IMMEDIATE
: $i8x16.MAX_U                    ( 79 )   121 253 2INST,             ; IMMEDIATE
: $F64x2.TRUNC                    ( 7A )   122 253 2INST,             ; IMMEDIATE
: $I8x16.AVGR_U                   ( 7B )   123 253 2INST,             ; IMMEDIATE
: $I16x8.EXTADD_PAIRWISE_I8x16_S  ( 7C )   124 253 2INST,             ; IMMEDIATE
: $I16x8.EXTADD_PAIRWISE_I8x16_U  ( 7D )   125 253 2INST,             ; IMMEDIATE
: $I16x8.EXTADD_PAIRWISE_I16x8_S  ( 7E )   126 253 2INST,             ; IMMEDIATE
: $I16x8.EXTADD_PAIRWISE_I16x8_U  ( 7F )   127 253 2INST,             ; IMMEDIATE

\ FD 80 1-> FD 8F 1 ------
: $I16x8.ABS                      ( 80 ) 1 128 253 3INST,             ; IMMEDIATE
: $I16x8.NEG                      ( 81 ) 1 129 253 3INST,             ; IMMEDIATE
: $I16x8.Q15MULR_SAT_S            ( 82 ) 1 130 253 3INST,             ; IMMEDIATE
: $I16x8.ALL_TRUE                 ( 83 ) 1 131 253 3INST,             ; IMMEDIATE
: $I16x8.BITMASK                  ( 84 ) 1 132 253 3INST,             ; IMMEDIATE
: $I16x8.NARROW_I32x4_S           ( 85 ) 1 133 253 3INST,             ; IMMEDIATE
: $I16x8.NARROW_I32x4_U           ( 86 ) 1 134 253 3INST,             ; IMMEDIATE
: $I16x8.EXTEND_LOW_I8x16_S       ( 87 ) 1 135 253 3INST,             ; IMMEDIATE
: $I16x8.EXTEND_HIGH_I8x16_S      ( 88 ) 1 136 253 3INST,             ; IMMEDIATE
: $I16x8.EXTEND_LOW_I8x16_U       ( 89 ) 1 137 253 3INST,             ; IMMEDIATE
: $I16x8.EXTEND_HIGH_I8x16_U      ( 8A ) 1 138 253 3INST,             ; IMMEDIATE
: $I16x8.SHL                      ( 8B ) 1 139 253 3INST,             ; IMMEDIATE
: $I16x8.SHR_S                    ( 8C ) 1 140 253 3INST,             ; IMMEDIATE
: $I16x8.SHR_U                    ( 8D ) 1 141 253 3INST,             ; IMMEDIATE
: $I16x8.ADD                      ( 8E ) 1 142 253 3INST,             ; IMMEDIATE
: $I16x8.ADD_SAT_S                ( 8F ) 1 143 253 3INST,             ; IMMEDIATE

\ FD 90 1-> FD 9F 1 ------
: $I16x8.ADD_SAT_U                ( 90 ) 1 144 253 3INST,             ; IMMEDIATE
: $I16x8.SUB                      ( 91 ) 1 145 253 3INST,             ; IMMEDIATE
: $I16x8.SUB_SAT_S                ( 92 ) 1 146 253 3INST,             ; IMMEDIATE
: $I16x8.SUB_SAT_U                ( 93 ) 1 147 253 3INST,             ; IMMEDIATE
: $F64x2.NEAREST                  ( 94 ) 1 148 253 3INST,             ; IMMEDIATE
: $i16x8.MUL                      ( 95 ) 1 149 253 3INST,             ; IMMEDIATE
: $i16x8.MIN_S                    ( 96 ) 1 150 253 3INST,             ; IMMEDIATE
: $i16x8.MIN_U                    ( 97 ) 1 151 253 3INST,             ; IMMEDIATE
: $i16x8.MAX_S                    ( 98 ) 1 152 253 3INST,             ; IMMEDIATE
: $i16x8.MAX_U                    ( 99 ) 1 153 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( 9A )
: $I16x8.AVGR_U                   ( 9B ) 1 155 253 3INST,             ; IMMEDIATE
: $I16x8.EXTMUL_LOW_I8x16_S       ( 9C ) 1 156 253 3INST,             ; IMMEDIATE
: $I16x8.EXTMUL_HIGH_I8x16_S      ( 9D ) 1 157 253 3INST,             ; IMMEDIATE
: $I16x8.EXTMUL_LOW_I8x16_U       ( 9E ) 1 158 253 3INST,             ; IMMEDIATE
: $I16x8.EXTMUL_HIGH_I8x16_U      ( 9F ) 1 159 253 3INST,             ; IMMEDIATE

\ FD A0 1-> FD AF 1
: $I32x4.ABS                      ( A0 ) 1 160 253 3INST,             ; IMMEDIATE
: $I32x4.NEG                      ( A1 ) 1 161 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( A2 )
: $I32x4.ALL_TRUE                 ( A3 ) 1 163 253 3INST,             ; IMMEDIATE
: $I32x4.BITMASK                  ( A4 ) 1 164 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( A5 )
\ UNASSIGNED                      ( A6 )
: $I32x4.EXTEND_LOW_I16x8_S       ( A7 ) 1 167 253 3INST,             ; IMMEDIATE
: $I32x4.EXTEND_HIGH_I16x8_S      ( A8 ) 1 168 253 3INST,             ; IMMEDIATE
: $I32x4.EXTEND_LOW_I16x8_U       ( A9 ) 1 169 253 3INST,             ; IMMEDIATE
: $I32x4.EXTEND_HIGH_I16x8_U      ( AA ) 1 170 253 3INST,             ; IMMEDIATE
: $I32x4.SHL                      ( AB ) 1 171 253 3INST,             ; IMMEDIATE
: $I32x4.SHR_S                    ( AC ) 1 172 253 3INST,             ; IMMEDIATE
: $I32x4.SHR_U                    ( AD ) 1 173 253 3INST,             ; IMMEDIATE
: $I32x4.ADD                      ( AE ) 1 174 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( AF )

\ B0-BF +
\ UNASSIGNED                      ( B0 )
: $I32x4.SUB                      ( B1 ) 1 177 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( B2 )
\ UNASSIGNED                      ( B3 )
\ UNASSIGNED                      ( B4 )
: $I32x4.MUL                      ( B5 ) 1 181 253 3INST,             ; IMMEDIATE
: $I32x4.MIN_S                    ( B6 ) 1 182 253 3INST,             ; IMMEDIATE
: $I32x4.MIN_U                    ( B7 ) 1 183 253 3INST,             ; IMMEDIATE
: $I32x4.MAX_S                    ( B8 ) 1 184 253 3INST,             ; IMMEDIATE
: $I32x4.MAX_U                    ( B9 ) 1 185 253 3INST,             ; IMMEDIATE
: $I32x4.DOT_I16x8_S              ( BA ) 1 186 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( BB )
: $I32x4.EXTEND_LOW_I16x8_S       ( BC ) 1 188 253 3INST,             ; IMMEDIATE
: $I32x4.EXTEND_HIGH_I16x8_S      ( BD ) 1 189 253 3INST,             ; IMMEDIATE
: $I32x4.EXTEND_LOW_I16x8_U       ( BE ) 1 190 253 3INST,             ; IMMEDIATE
: $I32x4.EXTEND_HIGH_I16x8_U      ( BF ) 1 191 253 3INST,             ; IMMEDIATE

\ C0-CF +
: $I64x2.ABS                      ( C0 ) 1 192 253 3INST,             ; IMMEDIATE
: $I64x2.NEG                      ( C1 ) 1 193 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( C2 )
: $I64x2.ALL_TRUE                 ( C3 ) 1 195 253 3INST,             ; IMMEDIATE
: $I64x2.BITMASK                  ( C4 ) 1 196 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( C5 )
\ UNASSIGNED                      ( C6 )
: $I64x2.EXTEND_LOW_I32x4_S       ( C7 ) 1 199 253 3INST,             ; IMMEDIATE
: $I64x2.EXTEND_HIGH_I32x4_S      ( C8 ) 1 200 253 3INST,             ; IMMEDIATE
: $I64x2.EXTEND_LOW_I32x4_U       ( C9 ) 1 201 253 3INST,             ; IMMEDIATE
: $I64x2.EXTEND_HIGH_I32x4_U      ( CA ) 1 202 253 3INST,             ; IMMEDIATE
: $I64x2.SHL                      ( CB ) 1 203 253 3INST,             ; IMMEDIATE
: $I64x2.SHR_S                    ( CC ) 1 204 253 3INST,             ; IMMEDIATE
: $I64x2.SHR_U                    ( CD ) 1 205 253 3INST,             ; IMMEDIATE
: $I64x2.ADD                      ( CE ) 1 206 253 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( CF )

\ D0-DF
\ UNASSIGNED                      ( D0 )
: $I64x2.SUB                      ( D1 ) 1 253 209 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( D2 )
\ UNASSIGNED                      ( D3 )
\ UNASSIGNED                      ( D4 )
: $I64x2.MUL                      ( D5 ) 1 253 213 3INST,             ; IMMEDIATE
: $I64x2.EQ                       ( D6 ) 1 253 214 3INST,             ; IMMEDIATE
: $I64x2.NE                       ( D7 ) 1 253 215 3INST,             ; IMMEDIATE
: $I64x2.LT_S                     ( D8 ) 1 253 216 3INST,             ; IMMEDIATE
: $I64x2.GT_S                     ( D9 ) 1 253 217 3INST,             ; IMMEDIATE
: $I64x2.LE_S                     ( DA ) 1 253 218 3INST,             ; IMMEDIATE
: $I64x2.GE_S                     ( DB ) 1 253 219 3INST,             ; IMMEDIATE
: $I64x2.EXTMULT_LOW_I32x4_S      ( DC ) 1 253 220 3INST,             ; IMMEDIATE
: $I64x2.EXTMULT_HIGH_I32x4_S     ( DD ) 1 253 221 3INST,             ; IMMEDIATE
: $I64x2.EXTMULT_LOW_I32x4_U      ( DE ) 1 253 222 3INST,             ; IMMEDIATE
: $I64x2.EXTMULT_HIGH_I32x4_U     ( DF ) 1 253 223 3INST,             ; IMMEDIATE

\ E0-EF
: $F32x4.ABS                      ( E0 ) 1 253 224 3INST,             ; IMMEDIATE
: $F32x4.NEG                      ( E1 ) 1 253 225 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( E2 )
: $F32x4.SQRT                     ( E3 ) 1 253 227 3INST,             ; IMMEDIATE
: $F32x4.ADD                      ( E4 ) 1 253 228 3INST,             ; IMMEDIATE
: $F32x4.SUB                      ( E5 ) 1 253 229 3INST,             ; IMMEDIATE
: $F32x4.MUL                      ( E6 ) 1 253 230 3INST,             ; IMMEDIATE
: $F32x4.DIV                      ( E7 ) 1 253 231 3INST,             ; IMMEDIATE
: $F32x4.MIN                      ( E8 ) 1 253 232 3INST,             ; IMMEDIATE
: $F32x4.MAX                      ( E9 ) 1 253 233 3INST,             ; IMMEDIATE
: $F32x4.PMIN                     ( EA ) 1 253 234 3INST,             ; IMMEDIATE
: $F32x4.PMAX                     ( EB ) 1 253 235 3INST,             ; IMMEDIATE
: $F64x2.ABS                      ( EC ) 1 253 236 3INST,             ; IMMEDIATE
: $F64x2.NEG                      ( ED ) 1 253 237 3INST,             ; IMMEDIATE
\ UNASSIGNED                      ( EE )
: $F64x2.SQRT                     ( EF ) 1 253 239 3INST,             ; IMMEDIATE

\ F0-FF
: $F64x2.ADD                      ( F0 ) 1 253 240 3INST,             ; IMMEDIATE
: $F64x2.SUB                      ( F1 ) 1 253 241 3INST,             ; IMMEDIATE
: $F64x2.MUL                      ( F2 ) 1 253 242 3INST,             ; IMMEDIATE
: $F64x2.DIV                      ( F3 ) 1 253 243 3INST,             ; IMMEDIATE
: $F64x2.MIN                      ( F4 ) 1 253 244 3INST,             ; IMMEDIATE
: $F64x2.MAX                      ( F5 ) 1 253 245 3INST,             ; IMMEDIATE
: $F64x2.PMIN                     ( F6 ) 1 253 246 3INST,             ; IMMEDIATE
: $F64x2.PMAX                     ( F7 ) 1 253 247 3INST,             ; IMMEDIATE
: $I32x4.TRUNC_SAT_F32x4_S        ( F8 ) 1 253 248 3INST,             ; IMMEDIATE
: $I32x4.TRUNC_SAT_F32x4_U        ( F9 ) 1 253 249 3INST,             ; IMMEDIATE
: $F32x4.CONVERT_I32x4_S          ( FA ) 1 253 250 3INST,             ; IMMEDIATE
: $F32x4.CONVERT_I32x4_U          ( FB ) 1 253 251 3INST,             ; IMMEDIATE
: $I32x4.TRUNC_SAT_F64x2_S_ZERO   ( FC ) 1 253 252 3INST,             ; IMMEDIATE
: $I32x4.TRUNC_SAT_F64x2_U_ZERO   ( FD ) 1 253 253 3INST,             ; IMMEDIATE
: $F64x2.CONVERT_LOW_I32x4_S      ( FE ) 1 253 254 3INST,             ; IMMEDIATE
: $F64x2.CONVERT_LOW_I32x4_U      ( FF ) 1 253 255 3INST,             ; IMMEDIATE



