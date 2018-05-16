#!/usr/bin/python
'''
Access method for blocks.txt, created via Ida having one line per function containing
the address, the name and one or more addresses of basic blocks
'''
class blockDB():
    functions = {}
    function_list = []
    highest_address = None
    lowest_address = None
    class functionObj():
        def __init__(self, line):
            parts = line.split()
            self.address = int(parts[0], 16)
            self.final = None
            self.name = parts[1]
            self.blocks = []
            for address in parts[2:]:
                val = int(address, 16)
                #print 'functionObj add %x to %s' % (val, self.name)
                if val >= self.address:
                    self.blocks.append(val)
                else:
                    print 'not including %x as block in function: %s at %x, may be a jump' % (val, self.name, self.address)
                    pass

        def hasAddress(self, address):
            if self.final is not None:
                if address >= self.address and address < self.final:
                    return True
                else:
                    return False 
            else:
                # TBD hack for size of last function
                if address >= self.address and address < (self.blocks[len(self.blocks)-1]+100):
                    return True
                else:
                    return False

        class functionBlock():
            def __init__(self, function, block):
                self.function = function
                self.block = block
        
        ''' given an address of this function, get functionBlock (which includes the block number) '''    
        def getBlock(self, address):
            retval = None
            index = 0
            if not self.hasAddress(address):
                print 'block not in function?'
                return None
            while retval is None and (index+1) < len(self.blocks):
                if address < self.blocks[index+1]:
                    #print 'getBlocks compare %x to %x' % (address, self.blocks[index+1])
                    retval = index
                    # TBD clean this up, no longer needed?
                    if not address >= self.blocks[index]:
                        print 'block db getBlock fails first sanity test for %x between %x and %x' % (address,
                           self.blocks[index], self.blocks[index+1])
                        exit(1) 
                index += 1
            if retval is None:
                if self.final is None:
                    if address > (self.blocks[len(self.blocks)-1]+100):
                        # TBD 
                        print 'block db getBlock fails sanity test for %x last block is %x' % (address,
                             self.blocks[len(self.blocks)-1]+100)
                        exit(1)
                retval = index
            return self.functionBlock(self, retval)

    def __init__(self, blockfile):
        self.bf = open(blockfile, 'r')
        prev_function = None
        for line in self.bf:
            function = self.functionObj(line)
            self.function_list.append(function.address)
            self.functions[function.address] = function
            if function.address > self.highest_address and len(function.blocks)>0:
                self.highest_address = function.blocks[len(function.blocks)-1]
            if self.lowest_address is None:
                self.lowest_address = function.address
            if prev_function is not None:
                prev_function.final = function.address
            prev_function = function
            print 'got %s numblocks is %d' % (function.name, len(function.blocks))

    def getHighestAddress(self):
        return self.highest_address

    def getLowestAddress(self):
        return self.lowest_address

    def getFunction(self, address):
        if address in self.functions:
            return self.functions[address]             
        else:
            return None

    def newFunctionBlock(self, function, block):
        return self.functionObj.functionBlock(function, block)

    ''' what function contains the given address?'''
    def findFunction(self, address):
        num_funs = len(self.functions)
        for i in range(num_funs - 2):
            if address >= self.function_list[i] and address < self.function_list[i+1]:
                function = self.getFunction(self.function_list[i])
                block = function.getBlock(address)
                #return self.functionObj.functionBlock(function, block)
                return block
        return None
       
            
#bdb = blockDB('/home/mike/heartbleed/blocks.txt') 
            
            
