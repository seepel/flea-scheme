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
;;                                  Next dictionary entry
;;                                   ^
;;                                   |
;;                    +------+-----+-|----+-----+------+
;;                    | PFA? | CFA | LINK | LEN | NAME |
;;                    +------+-----+------+-----+------+
;;                             ^
;;                             |
;;                     +-----+-|----+------+
;;                     | CFA | LINK | 3DUP |
;;                     +-----+------+------+
;;                       ^
;;                       |
;;        +------+-----+-|----+------+-------+-|---+-|-+-|----+
;;        | PFA  | CFA | LINK | 6DOU | BLE\0 | DUP | + | EXIT |
;;        +------+-----+------+------+-------+-----+---+------+
;;                ^
;;                |
;; +------+-----+-|----+------+------+--------+-|------+-|------+------+
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
  (import "env" "debug"   (func $debug (param i32)))
  ;; Function table to hold primitive words
  (table (export "table") 0xc3 funcref)

  ;; Interpreter interface

  ;; Registers
  ;; data stack pointer 
  (global $sp (mut i32) (i32.const 0x4000)) 
  (global $rp (mut i32) (i32.const 0x8000)) ;; return stack pointer
  (global $ip (mut i32) (i32.const 0x8000)) ;; instruction pointer
  (global $w (mut i32) (i32.const 0)) ;; working register
  (global $here (mut i32) (i32.const 0x8000)) ;; dictionary pointer
  (global $latest (mut i32) (i32.const 0x8000)) ;; latest word

  (func $sp     (export "sp")        (result i32) (global.get $sp))
  (func $rp     (export "rp")        (result i32) (global.get $rp))
  (func $ip     (export "ip")        (result i32) (global.get $ip))
  (func $w      (export "w")         (result i32) (global.get $w))
  (func $here   (export "here")      (result i32) (global.get $here))
  (func $latest (export "latest")    (result i32) (global.get $latest))
  (func $at     (export "at") (param $addr i32) (result i32)
        (i32.load (local.get $addr)))

  (func $aligned (export "round2bytes") (param $u i32) (result i32)
        (i32.add (local.get $u) (i32.const 3))
        (i32.and (i32.const -4)))

  ;; (func $create (export "create")
  ;;               (param i32 $addr) (param i32 $len)
  ;;               (result i32)
  ;;   ;;                  Next dictionary entry
  ;;   ;;          tblidx  ^                   
  ;;   ;;          ^       |                  
  ;;   ;; +------+-|---+---|----+-----+------+------ - - - -
  ;;   ;; | PFA? | CFA | LINK   | LEN | NAME | Param field
  ;;   ;; +-|----+-----+--------+-----+------+------ - - - -
  ;;   ;;   |                                ^
  ;;   ;;   +--------------------------------+

  ;;   (local $*pfa  i32) (local $*cfa  i32) (local $*link i32)
  ;;   (local $*len  i32) (local $*name  i32)
  ;;   (local $pfa   i32)
  ;;   (local.set $*pfa (global.get $here))
  ;;   (local.set $*cfa (i32.add (global.get $here) (i32.const 4)))
  ;;   (local.set $*link (i32.add (global.get $here) (i32.const 8)))
  ;;   (local.set $*len (i32.add (global.get $here) (i32.const 12)))
  ;;   (local.set $*name (i32.add (global.get $here) (i32.const 13)))
  ;;   (call $aligned (i32.add (local.get $len) 1))
  ;;   (local.set $pfa (i32.add (local.get $*len)
  ;;                            (call $aligned (local.get $len))))
  ;;   (i32.store (local.get $*pfa) (local.get $pfa))
  ;;   ;; (i32.store (local.get $*cfa) (???))
  ;;   (i32.store (local.get $*link) (global.get $latest))
  ;;   (i8.store (local.get $*len) (local.get $len))
  ;;   (memory.copy (local.get $*name) (local.get $addr) (local.get $len))
  ;;   (global.set $latest (local.get $*cfa))
  ;;   (global.set $here (local.get $pfa))
  ;;   )

  (func $push (export "push") (param $x i32)
        (global.set $sp (i32.sub (global.get $sp) (i32.const 4)))
        (i32.store (global.get $sp) (local.get $x)))

  (func $2push (export "2push") (param $x i32) (param $y i32)
        (global.set $sp (i32.sub (global.get $sp) (i32.const 8)))
        (i32.store (global.get $sp)
                   (local.get $x))
        (i32.store (i32.add (global.get $sp) (i32.const 4))
                   (local.get $y)))

  (func $rpush (export "2rpush") (param $x i32)
        (global.set $rp (i32.sub (global.get $rp) (i32.const 4)))
        (i32.store (global.get $rp) (local.get $x)))

  (func $pop  (export "pop") (result i32)
        (i32.load (global.get $sp))
        (global.set $sp (i32.add (global.get $sp) (i32.const 4))))

  (func $2pop (export "2pop") (result i32) (result i32)
        (i32.load (global.get $sp))
        (i32.load (i32.add (global.get $sp) (i32.const 4)))
        (global.set $sp (i32.add (global.get $sp) (i32.const 8))))

  (func $rpop (export "rpop") (result i32)
        (i32.load (global.get $rp))
        (global.set $rp (i32.add (global.get $rp) (i32.const 4))))

  (func $2rpop (export "2rpop") (result i32) (result i32)
        (i32.load (global.get $rp))
        (i32.load (i32.add (global.get $rp) (i32.const 4)))
        (global.set $rp (i32.add (global.get $rp) (i32.const 8))))

  (type $word (func))

  (func $NEXT (export "NEXT")
        (call $debug (global.get $ip))
        (global.set $w (i32.load (global.get $ip)))
        (global.set $ip (i32.add (global.get $ip) (i32.const 4)))
        (call_indirect (type $word) (i32.load (global.get $w))))

  ;; Primitive words

  (func $QUIT (export "QUIT")
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

  (func $DUP (export "DUP")
        (call $push (i32.load (global.get $sp))))
  (elem (i32.const 0x3) $DUP)
  (data (i32.const 0x801c) "\03\00\00\00")
  (data (i32.const 0x8020) "\03" "DUP")
  (data (i32.const 0x8024) "\10\80\00\00")

  (func $+ (export "ADD")
        (call $2pop)
        (i32.add)
        (call $push))
  (elem (i32.const 0x4) $+)
  (data (i32.const 0x8028) "\04\00\00\00")
  (data (i32.const 0x802c) "\01" "+" "\00\00")
  (data (i32.const 0x8030) "\1c\80\00\00")

  (func $* (export "MUL")
        (call $2pop)
        (i32.mul)
        (call $push))

  (func $ENTER
        (call $rpush (global.get $ip))
        (global.set $ip (i32.load (i32.sub (global.get $w) (i32.const 4)))))
  (elem (i32.const 0x5) $ENTER)
  (data (i32.const 0x8034) "\05\00\00\00")
  (data (i32.const 0x8038) "\05" "ENTER" "\00\00")
  (data (i32.const 0x8040) "\28\80\00\00")

  (func $EXIT
        (global.set $ip (call $rpop)))
  (elem (i32.const 0x6) $EXIT)
  (data (i32.const 0x8044) "\06\00\00\00")
  (data (i32.const 0x8048) "\04" "EXIT" "\00\00\00")
  (data (i32.const 0x8050) "\34\80\00\00")

  ;; : SQUARE DUP * ;
  ;; (data (i32.const 0x8050) "\06\80\00\00")       ;; PFA
  ;; (data (i32.const 0x8054) "\05\00\00\00")       ;; CFA 
  ;; (data (i32.const 0x8058) "\06" "SQUARE" "\00") ;; NAME
  ;; (data (i32.const 0x8060) "\44\80\00\00")       ;; LINK
  ;; (data (i32.const 0x8064) "\1c\80\00\00")       ;; DUP
  ;; (data (i32.const 0x8068) "\28\80\00\00")       ;; +
  ;; (data (i32.const 0x806c) "\40\80\00\00")       ;; EXIT

  )
