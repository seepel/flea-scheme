;; A simple Indirectly threaded Forth interpreter in 32 bit WebAssembly
;;
;; 
;;
;;   pointer to previous word
;;    ^
;;    |
;; +--|------+---
;; | LINK    | 6 
;; +---------+---
;;    ^       len                         padding
;;    |
;; +--|------+------ - - -
;; | LINK    | 9  xt ...
;; +---------+------ - - -
;;    ^       len                                     padding
;;    |
;;    |
;;   LATEST
;;        
;;                                          pointer to previous word
;;                                           ^
;;                                           |
;;        +---+---+---+---+---+---+---+---+--|------+----+------+ - -
;;        | D | O | U | B | L | E | 0 | 6 | LINK    | xt | addr | ...   
;;        +---+---+---+---+---+---+---+---+---------+----+------- - -
;;                                                    ^
;;                                                    | 
;; +---+---+---+---+---+---+---+---+---+---+---+---+--|------+----+------+ - - 
;; | Q | U | A | D | R | U | P | L | E | 0 | 0 | 9 | LINK    | xt | addr | ... 
;; +---+---+---+---+---+---+---+---+---+---+---+---+---------+----+------+ - -
;;                                                             ^
;;                                                             |
;;                                                            LATEST
;; 
;; 
;; 
;;
;;
;; Each entry is broken up into a header, an xt and a body
;;
;; +------+---+--------+----------+-------------+-----------+--------------+
;; | link | 6 | DOUBLE | ENTER xt | addr of DUP | addr of + | addr of EXIT |
;; +------+---+--------+----------+-------------+-----------+--------------+
;; <----- header -----> <-- xt --> <---------------- body ----------------> 
;;
;; When it is called from the context of another word the address is
;; resolved to the xt for the word. In this case DOUBLE is a user
;; defined word so the xt is the table index for the ENTER word.
;; The xt in a dictionary entry is always an index into the table
;; pointing at a function so this xt is used with call_indirect.
;;
;; : QUADRUPLE DOUBLE DOUBLE ;
;;
;; +-----------------+
;; | xt             --> $enter  : DOUBLE DUP + ;
;; +-----------------+
;; | addr of DOUBLE  ---------> +--------------+
;; +-----------------+          | xt           -> $enter        
;; | addr of DOUBLE  |          +--------------+
;; +-----------------+          | addr of DUP --------> +----+
;; | addr of EXIT    |  $ip --> +--------------+        | xt -> $dup tbl index
;; +-----------------+          | addr of +    |        +----+
;;                              +--------------+
;;                              | addr of EXIT |
;;                              +--------------+
;;
;;
;;
;;
;; On the other hand, DUP is a primitive word so the xt is an index
;; into the table where the $dup function is stored.
;;

(module
  (import "env" "memory"  (memory $memory 1601))
  (import "env" "malloc" (func $malloc (param i32) (result i32)))
  (import "env" "free"   (func $free   (param i32)))
  (table (export "table") 0xc3 funcref)

  ;; Registers
  (global $sp (mut i32) (i32.const 0x4000))       ;; data stack pointer
  (global $rp (mut i32) (i32.const 0x8000))       ;; return stack pointer
  (global $ip (mut i32) (i32.const 0x8000))       ;; instruction pointer
  (global $w  (mut i32) (i32.const 0))            ;; working register
  (global $x (mut i32) (i32.const 0))             ;; execution register
          

  (type $word (func))

  (func $push (param $x i32)
    (global.set $sp (i32.sub (global.get $sp) (i32.const 4)))
    (i32.store (global.get $sp) (local.get $x)))

  (func $2push (param $x i32) (param $y i32)
    (global.set $sp (i32.sub (global.get $sp) (i32.const 8)))
    (i32.store (global.get $sp)
               (local.get $x))
    (i32.store (i32.add (global.get $sp) (i32.const 4))
               (local.get $y)))

  (func $pushr (param $x i32)
    (global.set $rp (i32.sub (global.get $rp) (i32.const 4)))
    (i32.store (global.get $rp) (local.get $x)))

  (func $tos (result i32)
    (i32.load (global.get $sp)))

  (func $pop (result i32)
    (i32.load (global.get $sp))
    (global.set $sp (i32.add (global.get $sp) (i32.const 4))))

  (func $2pop (result i32) (result i32)
    (i32.load (global.get $sp))
    (i32.load (i32.add (global.get $sp) (i32.const 4)))
    (global.set $sp (i32.add (global.get $sp) (i32.const 8))))

  (func $popr (result i32)
    (i32.load (global.get $rp))
    (global.set $rp (i32.add (global.get $rp) (i32.const 4))))

  (func $QUIT
    (global.set $sp (i32.const 0x4000))
    (global.set $rp (i32.const 0x8000))
    )
  (data (i32.const 0x8000) "\00\00\00\00" "QUIT   " "\04")
  (data (i32.const 0x800C) "\01\00\00\00")
  (elem (i32.const 0x1) $QUIT)

  (func $LIT
    (call $push (i32.load (i32.add (global.get $w) (i32.const 4)))))
  (data (i32.const 0x8010) "\00\80\00\00" "LIT" "\83")
  (data (i32.const 0x8018) "\02\00\00\00")
  (elem (i32.const 0x2) $LIT)

  (func $DUP
    (global.set $sp (i32.sub (global.get $sp) (i32.const 4)))
    (i32.load (i32.add (global.get $sp) (i32.const 4)))
    (i32.store (global.get $sp)))
  (data (i32.const 0x801c) "\10\80\00\00" "DUP" "\03")
  (data (i32.const 0x8024) "\03\00\00\00")
  (elem (i32.const 0x3) $DUP)

  (func $*
    (call $2pop)
    (i32.mul)
    (call $push))
  (data (i32.const 0x8028) "\1c\80\00\00" "*  " "\01")
  (data (i32.const 0x802c) "\04\00\00\00")
  (elem (i32.const 0x4) $*)

  (func $ENTER
    (call $pushr (global.get $ip))
    (global.set $ip (i32.add (global.get $w) (i32.const 4))))
  (data (i32.const 0x8030) "\28\80\00\00" "ENTER  " "\05")
  (data (i32.const 0x803c) "\05\00\00\00")
  (elem (i32.const 0x5) $ENTER)

  (func $EXIT
    (global.set $ip (call $popr)))
  (data (i32.const 0x8040) "\34\80\00\00" "EXIT   " "\04")
  (data (i32.conts 0x804c) "\06\00\00\00")
  (elem (i32.const 0x6) $EXIT)

  (data (i32.const 0x8050) "\44\80\00\00" "SQUARE " "\06")
  (data (i32.const 0x805c) 
        "\05\00\00\00"                ;; $ENTER
        "\10\80\00\00" "\02\00\00\00" ;; $LIT 2
        "\28\80\00\00"                ;; $*
        "\44\80\00\00")               ;; $EXIT 

  (func $NEXT
    (global.set $w (i32.load (global.get $ip)))
    (global.set $ip (i32.add (global.get $ip) (i32.const 4)))
    (call_indirect (type $word) (i32.load (global.get $w))))
    

  (data (i32.const 0x))

  (global $here (mut i32) (i32.const 0x7fff))     ;; dictionary pointer
  (global $latest (mut i32) (i32.const 0x7fff))   ;; latest word
)
