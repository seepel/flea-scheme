;; A simple Indirectly threaded Forth interpreter in 32 bit WebAssembly
;;
;; Each entry is broken up into a header and a body.
;;
;; +-----+-----+------+------+---------+-------------+-----+
;; | PFA | CFA | LINK | 4NAM | E\0\0\0 | PARAM FIELD | ... |
;; +-----+-----+------+------+---------+-------------+-----+
;; <-------------- header ------------> <------ body ------> 
;;
;; The header consists of 
;; * The parameter field address (PFA)
;;   This is a pointer to the start of the body. Typically directly after the
;;   dictionary entry
;; * The code field address (CFA)
;;   This is an index into the wasm table of functions.
;; * The link field
;;   This is a pointer to the previous word in the dictionary. 
;; * The name length
;;   This is the length of the name of the word in a single byte.
;; * The name of the word
;;   This is the name of the word as a byte string. Typically utf8
;; * Padding
;;   The header is padded to a multiple of 4 bytes.
;;
;;
;; +===========================================================================+
;;  Dictionary Lookup
;; +===========================================================================+
;;  The dictionary is a linked list of words. Follow the link field to search
;;  th list.
;;
;;               Next dictoionary entry
;;                ^
;;                |
;; +------+-|---+-|----+-----+------+
;; | PFA? | CFA | LINK | LEN | NAME |
;; +------+-----+------+-----+------+
;;                ^
;;                |
;;        +-|---+-|----+------+
;;        | CFA | LINK | 3DUP |
;;        +-----+------+------+
;;                ^
;;                |
;; +------+-|---+-|----+------+-------+-|---+-|-+-|----+
;; | PFA  | CFA | LINK | 6DOU | BLE\0 | DUP | + | EXIT |
;; +------+-----+------+------+-------+-----+---+------+
;;                ^
;;                |
;; +------+-|---+-|----+------+------+--------+-|------+-|------+------+
;; | PFA  | CFA | LINK | 9QUA | DRUP | LE\0\0 | DOUBLE | DOUBLE | EXIT |
;; +-|----+-----+------+------+------+--------+--------+--------+------+
;;
;; +===========================================================================+
;;  Execution
;; +===========================================================================+
;;  Each cell in the parameter field is a pointer to a CFA in the dictionary.
;;  The CFA is an index into the wasm table of functions for the primitive
;;  words.
;;                                             
;;                                         +-----+------+------+
;;                                         | $DUP | LINK | 3DUP |
;;                                         +-----+------+------+
;;                                         ^
;;                                         |     +-----+------+--------+
;;                                         |     | $+ | LINK | 1+\0\0 |
;;                                         |     +-----+------+--------+
;;                                         |     ^
;;                                         |     |   +-------+------+------+---------+
;;                                         |     |   | $EXIT | LINK | 4EXI | T\0\0\0 |
;;                                         |     |   +-------+------+------+---------+
;;         $ENTER                          |     |   ^
;;          ^                              |     |   |
;; +------+-|------+------+------+-------+-|---+-|-+-|----+
;; | PFA  | $ENTER | LINK | 6DOU | BLE\0 | DUP | + | EXIT |
;; +-|----+--------+------+------+-------+-----+---+------+
;;   +-----------------------------------^
;; 
;; NOTE! The PFA for a word doesn't have to point to the end of the dictionary
;;       entry. It can point to any cell in memory. This allows some
;;       implementation flexibility.
;;
;; +-----+---+------+
;; | DUP | + | EXIT |
;; +-----+---+------+
;;   ^
;;   |
;; +-|---+-----+------+------+-------+
;; | PFA | CFA | LINK | 4DOU | BLE\0 |
;; +-----+-----+------+------+-------+

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
    (global.set $rp (i32.const 0x8000)))
  (elem (i32.const 0x1) $QUIT)
  (data (i32.const 0x8000) "\01\00\00\00")             ;; CFA
  (data (i32.const 0x8004) "\00\00\00\00")             ;; LINK
  (data (i32.const 0x8008) "\04" "QUIT" "\00\00\00")   ;; NAME
  

  (func $LIT
    (call $push (i32.load (i32.add (global.get $w) (i32.const 4)))))
  (elem (i32.const 0x2) $LIT)
  (data (i32.const 0x8010) "\02\00\00\00")
  (data (i32.const 0x8014) "\03" "LIT")
  (data (i32.const 0x8018) "\00\80\00\00")

  (func $DUP
    (global.set $sp (i32.sub (global.get $sp) (i32.const 4)))
    (i32.load (i32.add (global.get $sp) (i32.const 4)))
    (i32.store (global.get $sp)))
  (elem (i32.const 0x3) $DUP)
  (data (i32.const 0x801c) "\03\00\00\00")
  (data (i32.const 0x8020) "\03" "DUP")
  (data (i32.const 0x8024) "\10\80\00\00")
  

  (func $*
    (call $2pop)
    (i32.mul)
    (call $push))
  (elem (i32.const 0x4) $*)
  (data (i32.const 0x8028) "\04\00\00\00")
  (data (i32.const 0x802c) "\01" "*" "\00\00")
  (data (i32.const 0x8030) "\1c\80\00\00")

  (func $ENTER
    (call $pushr (global.get $ip))
    (global.set $ip (i32.load (i32.sub (global.get $w) (i32.const 4)))))
  (elem (i32.const 0x5) $ENTER)
  (data (i32.const 0x8034) "\05\00\00\00")
  (data (i32.const 0x8038) "\05" "ENTER" "\00\00")
  (data (i32.const 0x8040) "\28\80\00\00")

  (func $EXIT
    (global.set $ip (call $popr)))
  (elem (i32.const 0x6) $EXIT)
  (data (i32.const 0x8044) "\06\00\00\00")
  (data (i32.const 0x8048) "\04" "EXIT" "\00\00\00")
  (data (i32.const 0x8050) "\34\80\00\00")

  ;; : SQUARE DUP * ;
  (data (i32.const 0x8050) "\06\80\00\00")       ;; PFA
  (data (i32.const 0x8054) "\05\00\00\00")       ;; CFA 
  (data (i32.const 0x8058) "\06" "SQUARE" "\00") ;; NAME
  (data (i32.const 0x8060) "\44\80\00\00")       ;; LINK
  (data (i32.const 0x8064) "\1c\80\00\00")       ;; DUP
  (data (i32.const 0x8068) "\28\80\00\00")       ;; *
  (data (i32.const 0x806c) "\40\80\00\00")       ;; EXIT


  (func $NEXT
    (global.set $w (i32.load (global.get $ip)))
    (global.set $ip (i32.add (global.get $ip) (i32.const 4)))
    (call_indirect (type $word) (i32.load (global.get $w))))
    

  (global $here (mut i32) (i32.const 0x7fff))     ;; dictionary pointer
  (global $latest (mut i32) (i32.const 0x7fff))   ;; latest word
)