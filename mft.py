#!/usr/bin/python
import re
from Tkinter import *
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
master = Tk()
w = Canvas(master, width = 1024, height = 300)
w.pack()
def myLoop(the_file, delay, t, w):
    line = the_file.readline()
    operation, address, moved = t.parseLine(line)
    #if operation is not None:
    #    print 'myloop op: %s  addr: %x  moved: %d' % (operation, address, moved)
    if operation == 'Write' and address > lower and address < upper:
        offset = address - lower
        y = offset/1024
        x = offset % 1024
        w.create_line(x,y, x+moved, y)
        print 'do line at %d,%d   %d,%d' % (x, y, x+moved, y)
        master.update_idletasks()
    if len(line) > 0:
        master.after(0, myLoop, the_file, delay, t, w)
    
    

t = traces()
f = open('traceData.txt' , 'r')
highest = 0
myLoop(f, 400, t, w)
master.mainloop()

