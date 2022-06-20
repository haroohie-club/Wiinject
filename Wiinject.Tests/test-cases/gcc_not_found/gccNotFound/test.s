hook_80024000:
	stwu 1,-24(1)
	mflr 0
	stw 0,20(1)
	stw 31,16(1)
	mr 31,1
	mr 3,26
	bl =test_function
	lwz 0,20(1)
	mtlr 0
	addi 11,31,24
	lwz 31,-4(11)
	mr 1,11
	blr