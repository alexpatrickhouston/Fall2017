// 229turingmachine.cpp : Defines the entry point for the console application.
//
#pragma once
#include "stdafx.h"
#include "turingmachine.h"
#include <string>
#include <list>
#include <iostream>
#include <vector>
using namespace std;

int main()
{
	list<string> transitions;
	list<string> inputStrings;
	string line;
	std::getline(cin, line);
	std::getline(std::cin, line);
	std::getline(std::cin, line);
	//Grab the transitions
	while (line.find(',') != -1)//find a comma
	{
		transitions.push_back(line);
		std::getline(std::cin, line);
	}
	//grab the input strings
	do
	{
		inputStrings.push_back(line);
	} while (getline(cin, line));
	//read input file

	turingmachine tm(transitions.front[0]);
	for each(string t in transitions)
	{
		//each transistion includes current state, action, data char, next state
		//S, read, a ,A
		vector<string> parts = SplitString(t);
		tm.AddTransition(parts[0][0], parts[1], parts[2][0], parts[3][0]);

	}

	tm.SetFinalState('H');

	//create DFA
	for each (std::string s in inputStrings)
	{
		if (tm.Accept(s))
		{
			cout << "Accept: \t" << s << endl;
		}
		else
		{
			cout << "Not Accepted:<<" << s << endl;
		}
	}
	//process each string and print whether accept or reject

    return 0;
}

vector <string> SplitString(string input)
{
	vector<string> result;
	size_t i = input.find(", ");
	while (i != -1)
	{
		result.push_back(input.substr(0, i));
		input = input.substr(i + 2);
		i = input.find(", ");
	}
	result.push_back(input);
	return result;
}

