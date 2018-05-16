#!/usr/bin/python
import blockDB
import sys
import re
import os
#import kazoo
import fnmatch
import socket
import logging

here = '/mnt/cgcsvn/cgc/users/mft/zk/py'
if here not in sys.path:
    sys.path.append(here)
import szk
import configMgr
import utils
'''
Read a simics trace file and generate the operations.txt containing each call and goto
'''
class controlFlow():
    # current_block is of type blockDB.functionBlock, which has two fields, function object
    # and block number
    current_block = None
    function_trace = []
    highest_address = None
    lowest_address = None
    file_position = []
    start_clock = None
    def __init__(self, tracefile, blockfile, start_at):
        self.trace = open(tracefile)
        self.block_db = blockDB.blockDB(blockfile)
        self.highest_address = self.block_db.getHighestAddress()
        self.lowest_address = self.block_db.getLowestAddress()
        print 'highest address is %x, lowest: %x' % (self.highest_address, self.lowest_address)
        self.firstInstruction(start_at)

    def firstInstruction(self, start_at):
        done = False
        while not done:
            address, clock, eip = self.nextInstruction()
            if address is None:
                print 'firstInstruction found address of None, exiting'
                exit(1)
            if clock < start_at:
                continue
            if self.goodAddress(address):
                function = self.block_db.findFunction(address).function
                print 'firstInstruction found function %s addr %x for %x' % (function.name, function.address, address)
                print 'num blocks is %d' % len(function.blocks)
                #for address in function.blocks:
                #    print 'addr: %x' % address
                if function is not None:
                    done = True
                    self.current_block = function.getBlock(address)
                    self.start_clock = clock
                    self.eip = eip
                    print 'found for %x' % address
                else:
                    print 'not %x' % address
                    pass

    def bookmark(self):      
        self.file_position.append(self.trace.tell())
        if len(self.file_position) > 5:
            self.file_position.pop(0)

    def rewind(self):
        if len(self.file_position) > 0:
            #print 'rewinding'
            self.trace.seek(self.file_position.pop())

    def nextInstruction(self):
        address = None
        clock = None
        eip = None
        while address is None:
            line = self.trace.readline()
            if len(line) == 0:
                print 'end of file in parseTrace... done?'
                exit(0)
            if line.startswith('inst:'):
                address, clock, eip = self.addressAndClock(line)
        return address, clock, eip

    def showCurrentFunction(self):
        #print 'function: %s  addr: %x' % (self.current_block.name, self.current_block.function_address)
        pass 

    def nextOperation(self):
        operation = None
        destination = None
        clock = None
        eip = None
        while operation is None: 
            self.bookmark()
            t_operation, t_destination, clock, eip = self.nextTrace()
            if t_operation == 'step':
                #print 'step'
                self.showCurrentFunction()
                block = self.current_block.function.getBlock(t_destination)
                if block is None:
                    # we may have hit a jump table
                    #print 'seems to be a jump to %x' % t_destination
                    function = self.block_db.getFunction(t_destination)
                    if function is not None:
                        self.function_trace.append(self.current_block)
                        self.current_block = self.block_db.newFunctionBlock(function, 0)
                        operation = 'call'
                        destination = self.current_block
                    else:
                        #print 'nextOperation could not find function for %x' % t_destination
                        ''' must be call to library not in our blockDB, ignore it '''
                        pass
                elif block.block != self.current_block.block:
                    operation = 'goto'
                    self.current_block.block = block.block
                    destination = self.current_block
            elif t_operation == 'call':
                #print 'call to %x' % t_destination
                self.showCurrentFunction()
                function = self.block_db.getFunction(t_destination)
                if function is None:
                    print 'nextOperation no function for call to %x' % t_destination
                    function = self.block_db.findFunction(t_destination).function

                if function is None:
                    print 'nextOperation stillno function for call to %x' % t_destination
                    exit(1)
 
                self.function_trace.append(self.current_block)
                #print 'pushed %s %x' % (self.current_block.function.name, self.current_block.function.address)
                self.current_block = self.block_db.newFunctionBlock(function, 0)
                operation = t_operation
                destination = self.current_block
            elif t_operation == 'return': 
                #print 'is a return'
                self.showCurrentFunction()
                if len(self.function_trace) > 0:
                    self.current_block = self.function_trace.pop()
                    if self.current_block is None: 
                        print 'popped a none block!'
                        exit(1)
                    #print 'popped function now %s at %x' % (self.current_block.function.name, self.current_block.function.address)
                    if not self.current_block.function.hasAddress(t_destination):
                        function = self.block_db.findFunction(t_destination)
                        print 'return is not as expected popped %s, went to %s' % (self.current_block.function.name,
                            function.function.name)
                        self.rewind()
                        #exit(1)
                else:
                    print 'nowhere to return to finding function for address %x' % t_destination
                    self.current_block = self.block_db.findFunction(t_destination)
                #print 'is return'
                self.showCurrentFunction()
                operation = t_operation
                destination = self.current_block
            else:
               print 'unknown operation %s' % t_operation
        return operation, destination, clock, eip
       
    def goodAddress(self, address):
        if address < self.highest_address and address >= self.lowest_address:
            return True
        return False
 
    def nextTrace(self):
        done = False
        operation = None
        destination = None
        clock = None
        eip = None
        while operation is None:
            line = self.trace.readline()
            if(len(line) == 0):
                print 'no more lines, exiting?'
                exit(0)
            #print line
            if line.startswith('inst:'):
                #print 'LINE: %s' % line
                address, clock, eip = self.addressAndClock(line)
                call = line.find('call')
                if call > 0:
                    was = address
                    clock_was = clock
                    self.bookmark()
                    address, clock, eip = self.nextInstruction()
                    #print 'is call, destination is %x' % address
                    if self.goodAddress(address):
                        operation = 'call'
                        destination = address
                        self.rewind()
                        #print 'nextTrace call to %x addr was %x clock was %x' % (address, was, clock_was)
                else:
                    ret = line.find('ret')
                    if ret > 0:
                        was = address
                        self.bookmark()
                        address, clock, eip = self.nextInstruction()
                        if self.goodAddress(address):
                            #print 'ret to good address %x' % address
                            self.rewind()
                            if self.goodAddress(was):
                                operation = 'return'
                                #print 'nextTrace ret to %x, addr was %x' % (address, was)
                            else:
                                # return from untracked library, treat as a goto
                                operation = 'step'
                                self.showCurrentFunction()
                                #print 'nextTrace return converted to step to %x' % address
                                if not self.current_block.function.hasAddress(address):
                                    print 'nextTrace addr not in function, skipped return?'
                                    operation = 'return'
                                    self.rewind()
                            destination = address
                        else:
                            #print 'ret to bad address %x' % address
                            pass
                    else:
                        if self.goodAddress(address):
                            operation = 'step'
                            #print 'nextTrace step to %x' % address
                            destination = address
        return operation, destination, clock, eip
    def addressAndClock(self, s):
        address = None
        eip = None
        value = s[s.find("<")+1:s.find(">")]
        try:
            address = int(value[2:], 16)
        except:
            print 'bad address in %s' % s
            exit(1)
        eip = address
        value = s[s.find("[")+1:s.find("]")]
        try:
            clock = int(value, 16)
        except:
            print 'bad clock in %s' % s
            exit(1)
        return address, clock, eip
         

    def getCurrent(self):
        return self.current_block, self.start_clock
viz_project = '/Volumes/disk2/cgc/cgc/users/mft/simics/visualization/datasets'
block_file = None
trace_file = None
ops_file = None
start_at = 0
if len(sys.argv) == 3:
    replay = sys.argv[1]
    cb_bin = sys.argv[2]
    hostname = socket.gethostname()
    cfg = configMgr.configMgr()
    zk = szk.szk(hostname, cfg, cb_dir='/mnt/vmLib/cgcArtifacts')
    common = utils.getCommonName(cb_bin)
    path, cb = zk.replayPathFromNameArtifacts(replay, common)
    art_path = os.path.dirname(path)
    cb_path = zk.pathFromName(common) 
    block_file = os.path.join(os.path.dirname(cb_path),'ida', cb_bin, 'blocks.txt')
    trace_file = os.path.join(art_path, cb_bin, replay+'.txt')
    #viz_path = vizUtils.getVizPath(viz_project, replay, cb_bin)
    ops_file = os.path.join(art_path, 'operation.txt')
elif len(sys.argv) >= 4:
    trace_file = sys.argv[1]
    block_file = sys.argv[2]
    ops_file = sys.argv[3]
    if len(sys.argv) == 5:
       start_at = int(sys.argv[4], 16)
else:
    print 'controlFlow.py replay cb_bin'
    exit(1)

cf = controlFlow(trace_file, block_file, start_at)
ops = open(ops_file, 'w')
calls = 0
rets = 0
current, clock = cf.getCurrent()
ops.write('%x %x start %s %x %d\n' % (clock, current.function.address, current.function.name, current.function.address, current.block))
#for i in range(100000):
done = False
while not done:
    operation, destination, clock, eip =  cf.nextOperation()
    if operation is None:
        done = true
        continue
    if operation == 'call':
        ops.write('%x %x call %s %x\n' % (clock, eip, destination.function.name, destination.function.address))
        #print('%x call %s %x\n' % (clock, destination.function.name, destination.function.address))
        calls += 1 
    elif operation == 'return':
        ops.write('%x %x return %s %x %d\n' % (clock, eip, destination.function.name, destination.function.address,
              destination.block)) 
        #print('%x return %s %x %d\n' % (clock, destination.function.name, destination.function.address, destination.block)) 
        rets += 1 
    elif operation == 'goto':
        ops.write('%x %x goto %s %x %d\n' % (clock, eip, destination.function.name, 
             destination.function.address, destination.block))
        #print('%x goto %s %x %d\n' % (clock, destination.function.name, destination.function.address, destination.block))
    #print 'calls: %d  rets: %d' % (calls, rets) 
