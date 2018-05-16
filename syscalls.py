#!/usr/bin/python
import xml.etree.ElementTree as ET
'''
Access method for transmit/receive functions in callLog.xml file created by CGC forensics subsystem
'''
class syscalls():
    def __init__(self, callfile, isCB):
        self.read = 'read'
        self.write = 'write'
        if isCB:
            self.read = 'receive'
            self.write = 'transmit'
        self.exec_return = 'exec_return'
        self.mmap = 'mmap'
        self.munmap = 'munmap'
        self.accept = 'accept'
        self.care_about = [self.read, self.write, self.exec_return, self.mmap, self.munmap, self.accept]
        self.current_call = 0
        tree = ET.parse(callfile)
        self.root = tree.getroot()
        self.num_items = len(self.root)
        self.noMore = False;
        while self.root[self.current_call].tag != self.exec_return:
            self.current_call += 1
            if self.current_call >= self.num_items:
                self.noMore = True;
                break
        ''' 
        # skip ahead until a read or write
        while self.root[self.current_call].tag != self.read and self.root[self.current_call].tag != self.write:
            self.current_call += 1
            if self.current_call >= self.num_items:
                self.noMore = True;
                break
        if self.noMore:
            print 'NO read/write calls in callLog.txt'
        ''' 

    def getN(self, n):
        return self.root[n]

    def numItems(self):
        return self.num_items

    def nextItem(self):
        done = False;
        while not done:
            self.current_call += 1
            if self.current_call >= self.num_items:
                self.noMore = True;
                break
            #if self.root[self.current_call].tag == self.read or self.root[self.current_call].tag == self.write:
            if self.root[self.current_call].tag in self.care_about:
                done = True;

    def getClock(self):
        if self.noMore:
            return None
        item = self.getN(self.current_call)
        value = item[0].text
        hexval = int(value, 16)
        return hexval

    def getCurrent(self):
        if self.noMore:
            return None, None, None, None
        buf = 0
        count = 0
        item = self.getN(self.current_call)
        call = item.tag
        clock = self.getClock()
        eip = int(item[1].text, 16)
        if call == self.mmap or call == self.munmap:
            buf = int(item[2].text, 16)
            count = int(item[3].text, 16)
        elif call == self.exec_return:
            #print 'Exec return will be %s %s  ' % (item[2].text, item[3].text)
            if len(item) > 3:
                buf = int(item[2].text, 16)
                count = int(item[3].text, 16) 
            if len(item) > 4:
                print 'bss size is %s   ' % (item[5].text)
                count = count + int(item[5].text, 16) 
            print 'exec return buf %x  count %x' % (buf, count)
        elif call != self.accept:
            # skip the fd (TBD use when handling multiprocess CB)
            buf = int(item[3].text, 16)
            count = int(item[4].text)
        #print 'returning syscall %s eip %x clock %x' % (call, eip, clock)
        return call, clock, eip, buf, count
    
if __name__ == '__main__':    
    sc = syscalls('/mnt/simics/simicsWorkspace/call_logs/None_1_063.xml')
    for i in range(sc.numItems()):
        call, clock, eip, count = sc.getCurrent()
        print 'call: %s  clock: %x  eip: %x count: %d' % (call, clock, eip, count)
        sc.nextItem()
