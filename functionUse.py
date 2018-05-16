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
class functionUse():
    functions = []
    max_depth = 0
    cur_depth = 0
    previous = 'start'
    current = None
    previous = None
    stack = []
    biggest = []
    block_db = None
    class function():
        def __init__(self, name, address, level):
            self.name = name
            self.address = address
            self.level = level
            self.calls = []
            self.recurs_list = []
    	    self.recurs = False
    def __init__(self, operation_file, block_db_file):
        self.block_db = blockDB.blockDB(block_db_file)
        self.of = open(operation_file, 'r')
        for line in self.of:
            parts = line.split()
            if parts[2] == 'start':
                fun = self.function(parts[3], parts[4], 0)
                self.functions.append(fun)
                self.current = fun
                
            elif parts[2] == 'call':
                self.stack.append(self.current)
                fun = self.getFunction(parts[3])
                if fun is None:
                    fun = self.function(parts[3], parts[4], 0)
                    self.functions.append(fun)
                if self.current is not None and fun.name not in self.current.calls:
                    self.current.calls.append(fun.name)
                if fun in self.stack:
                    print 'recurs from %s to %s' % (self.current.name, fun.name)
                    #for fun in self.stack:
                    #    print fun.name
                    self.current.recurs_list.append(fun)
                self.current = fun
                self.cur_depth += 1
                if self.cur_depth > self.max_depth:
                    self.max_depth = self.cur_depth
                    self.biggest = list(self.stack)
                self.previous = self.current
            elif parts[2] == 'return':
                if self.cur_depth > 0:
                    self.cur_depth -= 1
                self.current = self.stack.pop()

    
    def getFunction(self, name):
        for fun in self.functions:
            if fun.name == name:
                return fun
        return None

    def numFunctions(self):
        print len(self.functions)
        print 'max depth %d' % self.max_depth

    def listCalls(self):
        for fun in self.functions:
            print '%s:%d %s' % (fun.name, fun.level, ' '.join([str(item) for item in fun.calls]))

    def dumpfile(self, fname):
        f = open(fname, 'w')
        for fun in self.functions:
            bfun = self.block_db.getFunction(int(fun.address,16))
            if bfun is not None:
                print 'write function %s' % fun.name
                f.write('%s %d %d %s\n' % (fun.name, fun.level, len(bfun.blocks), fun.address))
            else:
                print 'NO function found in db for %s (%s)' % (fun.address, fun.name)
                exit(1)
        f.close()

    def assignLongest(self):
        i = 1
        for function in self.biggest:
            print 'function %s gets level %d' % (function.name, i)
	    function.level = i
            i += 1

    def cycleAll(self):
        self.assignLongest()
        done = False
        zeros = 99
        while not done:
            done = True
            zeros = 0
            for function in self.functions:
                if function.level > 0:
                    for call in function.calls:
                        called = self.getFunction(call)
                        if called in function.recurs_list:
                            if called.level == 0:
                                called.level = function.level
                            else:
                                print '%s calls up to %s, do not adjust' % (function.name, called.name)
                        else:
                            print 'function %s of level %d calls %s of level %d' % (function.name,
                                function.level, called.name, called.level)
                            if called.level <= function.level:
                                called.level = function.level+1
                                print 'adjust %s to be %d' % (called.name,called.level)
                                done = False
                else:
                    zeros += 1
            print 'looped %d zeros' % zeros
                
        self.functions = sorted(self.functions, key=lambda function: function.level) 
    

viz_project = '/Volumes/disk2/cgc/cgc/users/mft/simics/visualization/datasets'
block_file = None
ops_file = None
fun_file = None


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
    #viz_path = vizUtils.getVizPath(viz_project, replay, cb_bin)
    fun_file = os.path.join(art_path, cb_bin,'functionList.txt')

elif len(sys.argv) >= 4:
    block_file = sys.argv[1]
    ops_file = sys.argv[2]
    fun_file = sys.argv[3]
else:
    print 'functionUse.py replay cb_bin'
    exit(1)

fu = functionUse(ops_file, block_file)

fu.numFunctions()
fu.cycleAll()
#fu.listCalls()
fu.dumpfile(fun_file)
                
