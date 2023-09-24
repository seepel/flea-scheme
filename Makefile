all: test

CC?=clang
LD?=wasm-ld
JS?=node

.PHONY: test

%.o: %.c
	$(CC) -DNDEBUG -Oz --target=wasm32 -nostdlib -c -o $@ $<

walloc.wasm: walloc.o vendor/walloc/walloc.o
	$(LD) --stack-first --global-base=104857601 --no-entry --import-memory -o $@ $^

waforth.wasm: waforth.wat
	wat2wasm $< -o $@

all: waforth.wasm walloc.wasm 

.PHONY: clean
clean:
	rm -f *.o *.wasm vendor/walloc/*.o vendor/walloc/*.wasm
