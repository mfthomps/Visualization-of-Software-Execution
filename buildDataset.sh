#!/bin/bash
./extractMoves.py $1 $2
./controlFlow.py $1 $2
./functionUse.py $1 $2
./combineDataSets.py $1 $2
