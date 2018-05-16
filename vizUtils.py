import os
def getVizPath(viz_project, replay, cb_bin):
    viz_path = os.path.join(viz_project, replay+'_VS_'+cb_bin)
    try:
        os.mkdir(viz_path)
    except:
        pass
    return viz_path
