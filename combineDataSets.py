#!/usr/bin/python
import blockMoves
import syscalls
import operations
import sys
import re
import os
#import kazoo
import fnmatch
import socket
import logging
import StringIO


here = '/mnt/cgcsvn/cgc/users/mft/zk/py'
if here not in sys.path:
    sys.path.append(here)
import szk
import configMgr
import utils
'''
Combine datasets into one combined.txt file with all desired operations.
The three input datasets are:
moveData.txt -- memory move instructions extracted from the raw trace via the extractMoves.py script
operation.txt -- function calls and block gotos extracted from raw trace via controlFlow.py
openssl_read_write.xml -- the syscalls 
'''
class combineDataSets():
    def __init__(self, move_file, call_file, ops_file, isCB):
        self.copy = blockMoves.blockMoves(move_file)
        self.calls = syscalls.syscalls(call_file, isCB)
        self.copy.nextItem()
        self.operations = operations.operations(ops_file)
        self.operations.nextItem()
	self.data_floor = 0x8282a80
	self.data_ceiling = self.data_floor + 0x1000000
        self.lowest_data = 0xffffffff
        self.highest_data = 0
        self.lowest_stack = 0xffffffff
        self.highest_stack = 0

    def nextClock(self, c1, c2, c3, s1, s2, s3):
        if c1 is None and c2 is None and c3 is None:
            return None
        #print 'not all none'
        if c1 is None and c2 is None:
            return s3
        if c1 is None and c3 is None:
            return s2
        if c2 is None and c3 is None:
            return s1
        #print 'not two none'
        if c1 is None:
            if c2 < c3:
                return s2
            else:
                return s3
        if c2 is None:
            if c1 < c3:
                return s1
            else:
                return s3
        #print 'not syscall  none'
        if c3 is None:
            if c1 < c2:
                return s1
            else:
                return s2
        if c1 < c2 and c1 < c3:
            return s1
        if c2 < c1 and c2 < c3:
            return s2
        else:
            return s3
            
     
    def nextType(self):
        copy_clock = self.copy.getClock() 
        calls_clock = self.calls.getClock() 
        ops_clock = self.operations.getClock() 
     
        item = self.nextClock(copy_clock, calls_clock, ops_clock, 'move', 'call', '')
        return item

    def inRange(self, source):
        if source >= self.data_floor and source <= self.data_ceiling:
            return True
        else:
            return False
    def setRanges(self, source, count):
        if source >= self.data_floor and source <= self.data_ceiling:
            if source < self.lowest_data:
                self.lowest_data = source
            if source+count > self.highest_data:
                self.highest_data = source+count
        elif source > self.data_ceiling:
            if source < self.lowest_stack:
                self.lowest_stack = source
            if source+count > self.highest_stack:
                self.highest_stack = source+count
       
    ''' TBD, for now only deal with data, no stack''' 
    def getBlockCopy(self):
        clock, eip, source, destination, count = self.copy.getCurrent()
        self.setRanges(source, count)
        self.setRanges(destination, count)
        self.copy.nextItem()
        if (source is None or self.inRange(source)) and self.inRange(destination):
            return clock, eip, source, destination, count
        else:
            return None, None, None, None, None

    def getCall(self):
        call, clock, eip, buf, count = self.calls.getCurrent()
        self.calls.nextItem()
        if True or self.inRange(buf):
            return call, clock, eip, buf, count
        else:
            return None, None, None, None, None

    def getOperation(self):
        clock, eip, op, function, fun_address, block = self.operations.getCurrent()
        self.operations.nextItem()
        return clock, eip, op, function, fun_address, block
       
    def showRanges(self):
        print 'data_floor: %x  data_ceiling is %x' % (self.data_floor, self.data_ceiling)
        size = self.highest_data - self.lowest_data
        print 'min data: %x max data: %x size: %x (%d)' % (self.lowest_data, self.highest_data, size, size) 
        size = self.highest_stack - self.lowest_stack
        print 'min stack: %x max stack: %x size: %x (%d)' % (self.lowest_stack, self.highest_stack, size, size) 
        ranges = open(ranges_file, 'w')
        ranges.write('min data: %x\n' % self.lowest_data)
        ranges.write('max data: %x\n' % self.highest_data)
        ranges.close()

viz_project = '/Volumes/disk2/cgc/cgc/users/mft/simics/visualization/datasets'
syscalls_file = None
move_file = None
ops_file = None
out_file = None
ranges_file = None
isCB = False;
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
    ops_file = os.path.join(art_path, cb_bin,'operation.txt')
    move_file = os.path.join(art_path, cb_bin,'moveData.txt')
    # TBD syscall logs are sequenced, reasonable way to select one?  take first
    syscalls_file = os.path.join(art_path, cb_bin,replay+'_000.xml')
    #viz_path = vizUtils.getVizPath(viz_project, replay, cb_bin)
    out_file = os.path.join(art_path, cb_bin,'combined.txt')
    ranges_file = os.path.join(art_path, cb_bin,'ranges.txt')
    config = ConfigParser.ConfigParser()

    cb_config = zk.getCBConfig(cb_bin)
    print 'config is %s' % config 
    cb_file = StringIO.StringIO(cb_config)
    config.readfp(cb_file)
    try:
        elf_data = int(config.get("elf", "data"), 16)
        elf_data_size = int(config.get("elf", "data_size"), 16)
    except ConfigParser.NoSectionError:
        print 'error reading elf values from config file for ' % cb_file
        exit(1)
    except ConfigParser.NoOptionError:
        print 'No data section, so no I/O or memory moves for now'
elif len(sys.argv) == 6:
    move_file = sys.argv[1] 
    syscalls_file = sys.argv[2] 
    ops_file = sys.argv[3] 
    out_file = sys.argv[4] 
    ranges_file = sys.argv[5]
else:
    print 'combineDataSets.py replay cb_bin'
    exit(1)

t = combineDataSets(move_file, syscalls_file, ops_file, isCB)
out = open(out_file, 'w')
done = False
while  not done:
    next_type = t.nextType()
    if next_type is None:
        done = True
    else:
        if next_type == 'move':
           clock, eip, source, destination, count =  t.getBlockCopy()
           if clock is not None:
               if source is not None:
                   out.write("%x %x move %x %x %d\n" % (clock, eip, source, destination, count))
               else:
                   out.write("%x %x move_local %x %d\n" % (clock, eip, destination, count))
        elif next_type == 'call':
           call, clock, eip, buf, count =  t.getCall()
           if clock is not None:
               out.write("%x %x %s %x %d\n" % (clock, eip, call, buf, count))
        else:
           clock, eip, op, function, fun_address, block = t.getOperation()
           out.write('%x %x %s %s %x %d\n' % (clock, eip, op, function, fun_address, block))
        
out.close()
t.showRanges()
