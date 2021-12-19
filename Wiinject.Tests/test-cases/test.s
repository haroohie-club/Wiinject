repl_8006FCA4:
    mr 6,28
    mr 7,27

repl_8006FCB0:
    mr 8,25

repl_8006FBA4:
    mr 6,25
    mr 7,26

repl_8006FBB0:
    mr 8,28

hook_80017250:
    add 5,5,0
    mr 26,3
    blr

hook_8001726C:
    stwu 1,-24(1)
    mflr 0
    stw 0,20(1)
    stw 31,16(1)
    mr 31,1
    stw 9,12(1)
    stw 3,8(1)
    mr 3,26
    lwz 0,20(1)
    mtlr 0
    mr 0,3
    lwz 9,12(1)
    lwz 3,8(1)
    addi 11,31,24
    lwz 31,-4(11)
    mr 1,11
    blr