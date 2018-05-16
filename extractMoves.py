#!/usr/bin/python
import sys
import re
import os
#import kazoo
import fnmatch
import socket
import logging
import vizUtils
here = '/mnt/cgcsvn/cgc/users/mft/zk/py'
if here not in sys.path:
    sys.path.append(here)
import szk
import configMgr
import utils
'''
Read a simics trace file and create a file containing all the block moves in this format:
clock move source destination quantity

A block move is any rep mov*

'''
class extract():
    ''' given a line, extract the EIP and the cpu cycle '''
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

    '''
    Return the operation, address, # bytes moved and clock from a given line
    '''
    def parseLine(self, s):
        operation = None
        address = None
        moved = None
        clock = None
        eip = None
        READ =  'Vani WB Read'
        WRITE = 'Vani WB Write'
        if s.startswith('data:'):
            address, clock, eip = self.addressAndClock(s)
            off = s.find(READ)
            operation = 'Read'
            if off < 0:
                off = s.find(WRITE)
                if off > 0:
                    operation = 'Write'
            if off > 0:
                #print 'off is %d' % off
                remain = s[off:].split()
                #print remain[3]
                try:
                    moved = int(remain[3])
                except:
                    print 'problem with line %s \n remain is %s' % (s, remain)
                    exit(1)
            else:
                operation = 'skip'
        elif s.startswith('inst:'):
            address, clock, eip = self.addressAndClock(s)
            if s.find('rep mov') > 0:
                operation = 'rep mov'
            elif s.find('mov') > 0:
                operation = 'mov'

        else:
            operation = 'skip'

        return operation, address, moved, clock, eip

dum_stack = 0xbffff000
dum_size = 0x5000000            
dum_min = dum_stack - dum_size
t = extract()
viz_project = '/Volumes/disk2/cgc/cgc/users/mft/simics/visualization/datasets'
trace_in = None
move_out = None
start_at = 0
if len(sys.argv) == 3:
    print 'finding trace file for %s' % sys.argv[1]
    replay = sys.argv[1]
    cb_bin = sys.argv[2]
    hostname = socket.gethostname()
    cfg = configMgr.configMgr()
    zk = szk.szk(hostname, cfg, cb_dir='/mnt/vmLib/cgcArtifacts')
    common = utils.getCommonName(cb_bin)
    path, cb = zk.replayPathFromNameArtifacts(replay, common)
    art_path = os.path.dirname(path)
    trace_in = os.path.join(art_path, cb_bin, replay+'.txt')
    #viz_path = vizUtils.getVizPath(viz_project, replay, cb_bin)
    move_out = os.path.join(art_path, 'moveData.txt')
elif len(sys.argv) >= 4:
    # arg 1 is 'paths' ? or some hack
    hostname = socket.gethostname()
    trace_in = sys.argv[2] 
    move_out = sys.argv[3] 
    if len(sys.argv) == 5:
        start_at = int(sys.argv[4], 16)

else:
    print 'extractMoves.py replay cb_bin'
    exit(1)

f = open(trace_in, 'r')
o = open(move_out, 'w')
done = False

def getNext():
    operation = None
    address = None
    moved = None
    clock = None
    eip = None
    done = False
    while not done:
        line = f.readline()
        if len(line) == 0:
            print 'done with file'
            o.close()
            exit(0)
        operation, address, moved, clock, eip = t.parseLine(line)
        if operation != 'skip':
            done = True
    if clock is None:
        print 'bad clock on line: %s' % line        
        exit(1)
    return operation, address, moved, clock, eip
   
'''
Only handles block moves, i.e., rep mov*
''' 
line = f.readline()
while not done:
    #line = f.readline()
    operation, address, moved, clock, eip = getNext()
    if clock < start_at:
        continue
    if operation == 'mov':
        instruct_address = address
        operation, address, moved, clock, dum = getNext()
        if operation == 'Write' and address < dum_min:
            start_write = address
            moved_size = moved
            clock_start = clock
            o.write('%x %x move_local %x %d\n' % (clock_start, eip, address, moved_size))

    if operation == 'rep mov':
        instruct_address = address
        operation, address, moved, clock, dum = getNext()
        if operation == 'Read':
            start_read = address
            clock_start = clock
            operation, address, moved, clock, dum = getNext()
            if operation == 'Write':
                start_write = address
                moved_size = moved
            else:
                print 'expected write, got %s' % operation
                exit(1)
            done_move = False
            while not done_move:
                operation, address, moved, clock, dum = getNext()
                if operation != 'Read':
                    done_move = True
                else:
                    operation, address, moved, clock, dum = getNext()
                    if operation != 'Write':
                        if operation == 'rep mov' and address == instruct_address:
                            # continuation of mov instruction after interrupt
                            continue
                        try:
                            print 'in move, expected write, got %s at clock %x' % (operation, clock)
                        except:
                            print 'in move, operation is %s  no good clock data' % operation
                        exit(1)
                    moved_size = moved_size + moved
            o.write('%x %x move %x %x %d\n' % (clock_start, eip, start_read, start_write, moved_size))
                   
               
o.close()
f.close()

