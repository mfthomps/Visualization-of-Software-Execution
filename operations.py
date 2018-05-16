'''
Read the operations.txt file and return clock/entry information for merging
'''
class operations():
    def __init__(self, opfile):
        self.f = open(opfile, 'r')
        self.clock = None
        self.eip = None
        self.op = None
        self.function = None
        self.fun_address = None
        self.block = None
    def nextItem(self):
        line = self.f.readline()
        #print line
        if len(line) == 0:
            self.clock = None
            self.eip = None
            self.source = None
            self.destination = None
            self.count = None
        else:
            items = line.split()
            self.clock = int(items[0], 16)
            self.eip = int(items[1], 16)
            self.op = items[2]
            self.function = items[3]
            self.fun_address = int(items[4], 16)
            if self.op == 'call':
                self.block = 0
            else:
                self.block = int(items[5])
    def getClock(self):
        return self.clock
    def getCurrent(self):
        return self.clock, self.eip, self.op, self.function, self.fun_address, self.block
