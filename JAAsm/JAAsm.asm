;
; Author: Blazej Sztefka
; Date: January 7, 2025, Year 2024/2025, Semester 1
; Project: Laplace Filter Implementation in Assembly
; Description: This program implements the Laplace filter, which is used to emphasize edges in an image by applying 
;              difference calculations between pixel intensities and their neighbors. The algorithm assumes that data 
;              is laid out as a continuous buffer, without specific handling for padding in rows. The result is stored 
;              in an output buffer.
; Version: 1.0
;

.code

; Procedure: ApplyLaplaceFilterAsm
; Description: Applies the Laplace filter on an input image buffer, emphasizing edges by performing differential 
;              operations on neighboring pixels. Note: This code does not explicitly account for row padding.
; Input Parameters:
;  - rcx: Pointer to the input buffer (uint8_t array).
;  - rdx: Pointer to the output buffer (uint8_t array).
;  - r8: Number of bytes per row, without any special padding consideration (rowByteCount).
;  - r9: Total number of bytes to process, including all rows (totalByteCount).
; Output:
;  - The processed data is written to the output buffer pointed to by rdx.
; Registers Modified:
;  - rbx, r10, r11, r12, r13, ymm0, ymm1, rax, rdx.
; Flags Modified:
;  - CF, OF, SF, ZF (depending on specific instructions like `cmp`, `add`, `sub`).
;

ApplyLaplaceFilterAsm proc
    push rbx                ; Save rbx (used for rowByteCount calculations)
    push r10                ; Save r10 (row counter)
    push r11                ; Save r11 (temporary usage)
    push r12                ; Save r12 (byte counter)
    push r13                ; Save r13 (additional byte counter)

    mov rsi, rcx            ; rsi = pointer to input buffer
    mov rdi, rdx            ; rdi = pointer to output buffer
    mov rbx, r8             ; rbx = rowByteCount (bytes per row, no padding)
    mov r9, r9              ; r9 = totalByteCount (bytes in entire buffer)
    mov r12, 16             ; r12 = byte counter (start at 16 bytes for SIMD)
    xor r10, r10            ; r10 = row counter (initialize to 0)
    xor r13, r13            ; r13 = additional byte counter (initialize to 0)
    lea rbx, [rbx*2 + rbx]  ; rbx = rowByteCount * 3 because of 3 channels (RGB)

check_the_end:              ; Check if all data has been processed
    cmp r12, r9             ; Compare current byte counter with total bytes
    jge end_loop            ; Jump to end if all data is processed
    sub r12, 16             ; Subtract 16 bytes (step size for SIMD processing)

copy_loop:                  ; Main loop for Laplace filter processing
    cmp r12, 0              ; Check if r12 is zero
    je add_3                ; skipping the first pixel to the left of the first modified row

    ; Load input data into xmm0
    movdqu xmm0, [rsi + r12] ; Load 16 bytes of input data into xmm0

    ; Expand xmm0 to ymm0 (8-bit to 16-bit unsigned integers)
    vpmovzxbw ymm0, xmm0

    ; Multiply by 4 (shift left by 2)
    vpslld ymm0, ymm0, 2    

    ; Load and subtract right neighbor (shifted by 3 bytes)
    movdqu xmm1, [rsi + r12 + 3] ; Load 16 bytes from right neighbor
    vpmovzxbw ymm1, xmm1         ; Expand xmm1 to ymm1
    vpsubw ymm0, ymm0, ymm1      ; Subtract right neighbor

    ; Load and subtract left neighbor (shifted by -3 bytes)
    movdqu xmm1, [rsi + r12 - 3] ; Load 16 bytes from left neighbor
    vpmovzxbw ymm1, xmm1         ; Expand xmm1 to ymm1
    vpsubw ymm0, ymm0, ymm1      ; Subtract left neighbor

    ; Load and subtract upper row neighbor
    mov rax, r12
    sub rax, rbx
    movdqu xmm1, [rsi + rax]     ; Load 16 bytes from right neighbor
    vpmovzxbw ymm1, xmm1         ; Expand xmm1 to ymm1
    vpsubw ymm0, ymm0, ymm1      ; Subtract upper row neighbor

    ; Load and subtract lower row neighbor
    mov rax, r12
    add rax, rbx
    movdqu xmm1, [rsi + rax]     ; Load 16 bytes from right neighbor
    vpmovzxbw ymm1, xmm1         ; Expand xmm1 to ymm1
    vpsubw ymm0, ymm0, ymm1      ; Subtract lower row neighbor

    ; Clamp values to [0, 255]
    vpxor ymm1, ymm1, ymm1       ; Zero ymm1
    vpmaxsw ymm0, ymm0, ymm1     ; Clamp values < 0 to 0
    vpcmpeqw ymm1, ymm1, ymm1    ; Set ymm1 to 0xFFFF
    vpsrlw ymm1, ymm1, 1         ; Clamp values > 255
    vpminsw ymm0, ymm0, ymm1     

    ; Pack back to 8-bit and store in output buffer
    vpmovuswb xmm0, ymm0         ; Compress lower 128 bits to xmm0
    movdqu [rdi + r12], xmm0     ; Store result in output buffer

    ; Update pointers and counters
    add r12, 32                  ; Increment pointer by 16 bytes
    add r12, r13                 ; Add additional counter to r12
    mov r13, 0                   ; Reset additional counter

    ; Handle row boundaries
    mov rax, r12                 ; Move byte counter to rax
    xor rdx, rdx                 ; Zero rdx
    div rbx                      ; Divide rax by rowByteCount
    cmp rax, r10                 ; Compare quotient with row counter
    je check_the_end             ; Continue if still within the row

    ; Prepare for new row
    add r10, 1                   ; Increment row counter
    sub r12, rdx                 ; Step back to get pixels only from the current row
    sub r12, 3                   ; we take into account the right edge pixel
    mov r13, 6                   ; Set additional counter to 6 to skip the first pixel of the next row and last pixel of the current row

    jmp check_the_end            ; Repeat loop

add_3:                           ; Handle edge case (add 3 bytes)
    add r12, 3                   ; Add 3 to byte counter
    jmp copy_loop                ; Continue processing

end_loop:                        ; Finalize processing
    mov rdx, rdi                 ; Move output pointer to rdx
    pop r13                      ; Restore r13 from stack
    pop r12                      ; Restore r12 from stack
    pop r11                      ; Restore r11 from stack
    pop r10                      ; Restore r10 from stack
    pop rbx                      ; Restore rbx from stack

    ret                          ; Return to caller
ApplyLaplaceFilterAsm endp
end
