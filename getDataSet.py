#!/usr/bin/python
'''
Get a set of post-processed files from the master monitor and put them into a visualization 
project directory.
'''
import subprocess
import os
import sys
import os
import socket

here = '../../zk/py'
if here not in sys.path:
    sys.path.append(here)
import szk
import configMgr
import utils
import vizUtils
remote_host = 'hp1:'
viz_project = '/Volumes/disk2/cgc/cgc/users/mft/simics/visualization/datasets'
def doSCP(source, destination):
    retcode = subprocess.call(['/usr/bin/scp','-o StrictHostKeyChecking=no', source, destination])

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
    ranges_file = os.path.join(art_path, cb_bin,'ranges.txt')
    functions_file = os.path.join(art_path, cb_bin,'functionList.txt')
    # TBD syscall logs are sequenced, reasonable way to select one?  take first
    syscalls_file = os.path.join(art_path, cb_bin,replay+'_000.xml')
    #viz_path = vizUtils.getVizPath(viz_project, replay, cb_bin)
    combined_file = os.path.join(art_path, cb_bin,'combined.txt')
    ranges_file = os.path.join(art_path, cb_bin,'ranges.txt')
    viz_path = vizUtils.getVizPath(viz_project, replay, cb_bin)
    doSCP(remote_host+block_file, os.path.join(viz_path, 'blocks.txt'))
    doSCP(remote_host+ops_file, os.path.join(viz_path, 'operation.txt'))
    doSCP(remote_host+move_file, os.path.join(viz_path, 'moveData.txt'))
    doSCP(remote_host+ranges_file, os.path.join(viz_path, 'ranges.txt'))
    doSCP(remote_host+syscalls_file, os.path.join(viz_path, replay+'_000.xml'))
    doSCP(remote_host+combined_file, os.path.join(viz_path, 'combined.txt'))
    doSCP(remote_host+functions_file, os.path.join(viz_path, 'functionList.txt'))
else:
    print 'getDataSet.py replay cb_bin'
    exit(1)
