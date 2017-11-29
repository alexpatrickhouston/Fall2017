#pragma once
#include "turingmachine.h"
#include "stdafx.h"
#include <stack>
#include <set>
#include <map>
#include <string>
using namespace std;
turingmachine::turingmachine(char ss)
{
	StartingState = ss;
	FinalState = 'H';
}
turingmachine::~turingmachine()
{
}
void turingmachine::SetFinalState(char finalstate)
{
}
bool turingmachine::Accept(string s)
{
	size_t head = 0;//head of the reader
	char currentState = initialState;
	string tape = s;
	string keyvalue;//The key value of the next state
	while (currentState != FinalState)
	{
		string key = MakeKey(currentState, tape[head]);//make a key based on current state and where the head is on the tape
		if (transitions.find(key) != transitions.end())//if it found a transition
		{
			keyvalue = transitions[key];//set the keyvalue to that key
			currentState = keyvalue[0];//grab the next state from the key value and set it to the current state
			tape[head] = keyvalue[1];//set the write letter to the char where the head is
			if (keyvalue[2] == 'R')//move the head
				head++;//if right move it right
			else
				head--;//else left
			if (head > s.length-1 || head <= 0)//if head is greater than string length -1 since arrays or <= 0 means we ran out our tape
				return false;
		}
		else
			return false;
	}
	return true;
}
string turingmachine::MakeKey(char currentstate, char inputchar)
{//Makes a key out of the starting state and input char
	string key;
	key += currentstate;
	key += inputchar;
	return key;
}
string turingmachine::MakeKeyValue(char nextstate, char outputchar, char dir)
{
	string key; // Key value looks like this nextstate|outputchar|dir
	key += nextstate;
	key += outputchar;
	key += dir;
	return key;
}
void turingmachine::AddTransition(char currentstate, char nextstate, char inputchar, char outputchar, char Dir)
{
	string key = MakeKey(currentstate, nextstate);//get the key for the transition
	string keyvalue = MakeKeyValue(inputchar, outputchar, Dir);//get the key value for the transition
	transitions[key] = keyvalue;//add the key with the value to the map
}


