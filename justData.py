#!/usr/bin/python
import re
class traces():
    def parseLine(self, s):
        operation = None
        address = None
        moved = None
        if s.startswith('data:'):
            value = s[s.find("<")+1:s.find(">")]
            #m = re.search(r"\[([A-Za-z0-9_]+)\]", line) 
            #print value[2:]
            address = int(value[2:], 16)
            off = s.find('Read')
            operation = 'Read'
            if off < 0:
                off = s.find('Write')
                if off > 0:
                    operation = 'Write'
            if off > 0:
                #print 'off is %d' % off
                remain = s[off:].split()
                #print remain[1]
                moved = int(remain[1])
        return operation, address, moved
            

lower = 0x8282a80
upper = lower + 0x5000000

t = traces()
f = open('/mnt/simics/simicsWorkspace/traces/trace.txt', 'r')
o = open('traceData.txt', 'w')
for line in f:
    operation, address, moved = t.parseLine(line)
    if operation is not None and address > lower and address < upper:
        o.write('%s' % line)
        print 'got %s' % line
o.close()
f.close()

