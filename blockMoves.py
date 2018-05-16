#!/usr/bin/python
class blockMoves():
    def __init__(self, movefile):
        self.f = open(movefile, 'r')
        self.clock = None
        self.source = None
        self.destination = None
        self.count = None
        self.eip = None
    def nextItem(self):
        line = self.f.readline()
        if len(line) == 0:
            self.clock = None
            self.source = None
            self.destination = None
            self.count = None
            self.eip = None
        else:
            items = line.split()
            self.clock = int(items[0], 16)
            self.eip = int(items[1], 16)
            if items[2] == 'move':
                self.source = int(items[3], 16)
                self.destination = int(items[4], 16)
                self.count = int(items[5])
            else:
                self.destination = int(items[3], 16)
                self.count = int(items[4])
                self.source = None
    def getClock(self):
        return self.clock
    def getCurrent(self):
        return self.clock, self.eip, self.source, self.destination, self.count 

if __name__ == '__main__':    
    bm = blockMoves('moveData.txt')
    done = False
    while not done:
        bm.nextItem()
        clock, source, destination, count = bm.getCurrent()
        if clock is None:
            done = True
        else:
            print 'clock: %x  source: %x  destination: %x  count: %d' % (clock, source, destination, count)
