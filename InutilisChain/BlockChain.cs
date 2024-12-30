using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace InutilisChain;

public class BlockChain
{
    private ReaderWriterLockSlim blockchainMutex = new ReaderWriterLockSlim();
    private List<Block> blocks;
    public delegate void BlockMinedEventHandler(Block newBlock);
    public event BlockMinedEventHandler onBlockAdded;

    public bool canBeAdded(Block newBlock)
    {
        if (blocks.Count == 0)
            return true;
        return isValidBlock(getLastBlock(), newBlock);
    }

    public bool isInBlockchain(Block block)
    {
        return blocks.Contains(block);
    }
    
    public List<Block> getBlockChain()
    {
        List<Block> block;
        blockchainMutex.EnterReadLock();
        block = blocks;
        blockchainMutex.ExitReadLock();
        return block;
    }

    public int getCount()
    {
        //blockchainMutex.EnterReadLock();
        int blocksCount = blocks.Count;
        //blockchainMutex.ExitReadLock();
        return blocksCount;
    }
    
    public int getLastIndex()
    {
        return getLastBlock().index;
    }
    
    public Block getBlock(int index)
    {
        Block block = null;
        blockchainMutex.EnterReadLock();
        block = blocks[index];
        blockchainMutex.ExitReadLock();
        return block;
    }
    
    public Block getLastBlock()
    {
        Block block = null;
        blockchainMutex.EnterReadLock();
        block = blocks.Last();
        blockchainMutex.ExitReadLock();
        return block;
    }
    
    public Block getBlockAt(int i)
    {
        return blocks[Math.Clamp(i, 0, int.MaxValue)];
    }

    public bool addBlock(Block block)
    {
        bool canBeAdded;
        blockchainMutex.EnterWriteLock();
        if (blocks.Count == 0)
        {
            canBeAdded = true;
            blocks.Add(block);
        }
        else
        {
            canBeAdded = isValidBlock(blocks.Last(), block);
            if(canBeAdded) 
                blocks.Add(block);
        }
        blockchainMutex.ExitWriteLock();
        if(canBeAdded)
            onBlockAdded?.Invoke(block);
        return canBeAdded;
    }

    public void setBlockChain(List<Block> blockChain)
    {
        blockchainMutex.EnterWriteLock();
        blocks = blockChain;
        blockchainMutex.ExitWriteLock();
        foreach (Block block in blockChain)
            onBlockAdded?.Invoke(block);
    }

    public BlockChain()
    {
        blocks = new List<Block>();
    }
    
    public static bool isValidBlock(Block lastBlock, Block newBlock)
    {
        //if (!VerifyTimeAgainstPreviousBlock(lastBlock, newBlock) || !VerifyTimeWithLocalTime(newBlock))
        //    return false;

        //PrintByteArray(newBlock.hash);
        // check difficulty
        for(int i = 0; i < newBlock.difficulty; i++)
            if (newBlock.hash[i] != '0')
                return false;

        if (newBlock.index != lastBlock.index + 1)
            return false;

        if (!newBlock.previousHash.SequenceEqual(lastBlock.hash))
            return false;
            
        return true;
    }
    public static bool VerifyTimeAgainstPreviousBlock(Block previousBlock, Block newBlock)
    {
        if (newBlock.timestamp - previousBlock.timestamp > 60)
            return false;
        return true;
    }
    
    public static bool VerifyTimeWithLocalTime(Block newBlock)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - newBlock.timestamp > 60)
            return false;
        return true;
    }
    
    public void Print()
    {
        foreach (Block block in blocks)
        {
            Console.WriteLine(block.Serialize());
        }
    }
}