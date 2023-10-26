using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

enum Mirror
{
	HORIZONTAL,
	VERTICAL,
	FOUR_SCREEN,
	SINGLE_SCREEN_LOWER_BANK,
	SINGLE_SCREEN_UPPER_BANK,
}

interface Stream
{
	int read(int address);
	void write(int address, int data);
}

interface ISerialize
{
	//virtual void Save(std::ostream& out){}
	//virtual void Load(std::istream& in){}
}

interface IDevice : ISerialize
{
	void clock();
}


interface IBus
{
	void writeByte(int address, int data);
	void writeWord(int address, int data);
	int readByte(int address);
	int readWord(int address);
}

class IROMInfo
{
	public int prg; // 16KB unit
	public int chr; // 8KB unit
	public int mapper; // mapper int
	public Mirror mirror;
	public bool hasBatteryBacked;
	public bool isTrained;
}

interface IDMA
{
	void copy(int cpuBusAddr);
}

interface ICPU : IDevice
{
	void reset();
	void irq();
	void nmi();

	IBus GetBus();
}

public interface IController
{
	void write(int data);
	int read();
}

public enum StandardControllerButton
{
	A = 0x80,
	B = 0x40,
	SELECT = 0x20,
	START = 0x10,
	UP = 0x08,
	DOWN = 0x04,
	LEFT = 0x02,
	RIGHT = 0x01,
}

public interface IStandardController : IController
{
	void updateButton(StandardControllerButton button, bool isPressDown);
}
public delegate void OnSample(float volume);
public delegate void OnFrame(List<int> frame);

public class IOptions
{
	public int sampleRate; // default 48000
	public OnSample onSample;
	public OnFrame onFrame;
	public List<int> sramLoad;
}

public abstract class IEmulator
{
	public IStandardController standardController1;
	public IStandardController standardController2;
	public List<int> sram;
	public abstract void frame();

	public abstract void clock();
}

interface IInterrupt
{
	void irq();
	void nmi();
}

abstract class IMapper
{
	public abstract void ppuClockHandle(int scanLine, int cycle);

	public abstract int read(int address);

	public abstract void write(int address, int data);
	public IInterrupt interrupt;
}

abstract class ICartridge
{
	public IROMInfo info;
	public IMapper mapper;
}



enum SpriteSize
{
	SIZE_8X8 = 8,
	SIZE_8X16 = 16,
}

abstract class IPPUController
{
	public int baseNameTableAddress; // One of [ 0x2000, 0x2400, 0x2800, 0x2C00 ]
	public int vramIncrementStepSize; // One of [ 1, 32 ]
	public int spritePatternTableAddress; // One of [ 0x0000, 0x1000 ], ignored in 8x16 mode
	public int backgroundPatternTableAddress; // One of [ 0x0000, 0x1000 ],
	public SpriteSize spriteSize;
	public bool isNMIEnabled;

	public virtual int data { get; set; }
}

abstract class IMask
{
	public bool isColorful;
	public bool isShowBackgroundLeft8px;
	public bool isShowSpriteLeft8px;
	public bool isShowBackground;
	public bool isShowSprite;
	public bool isEmphasizeRed;
	public bool isEmphasizeGreen;
	public bool isEmphasizeBlue;

	public virtual int data { get; set; }
}


abstract class IStatus
{
	public bool isSpriteOverflow;
	public bool isZeroSpriteHit;
	public bool isVBlankStarted;

	public abstract int data { get; }
}

abstract class IPPU : IDevice
{
	//List<int> pixels; // NES color array
	public abstract int cpuRead(int address);
	public abstract void cpuWrite(int address, int data);
	public abstract void dmaCopy(List<int> data);

	public abstract void clock();
}

abstract class IRAM
{

	public abstract int read(int address);

	public abstract void write(int address, int data);
}


internal abstract class IChannel
{
	public int volume;
	public int lengthCounter;

	bool isEnabled;

	public bool enable
	{
		get { return isEnabled; }
		set
		{
			isEnabled = value;
			if (!value)
				lengthCounter = 0;
		}
	}

	public virtual void clock() { }
	public virtual void processEnvelope() { }
	public virtual void processLinearCounter() { }
	public abstract void processLengthCounter();
	public virtual void processSweep() { }
	public virtual void write(int offset, int data) { }
}

abstract class IAPU
{

	public abstract void clock();

	public abstract int read(int address);
	public abstract void write(int address, int data);
}

public static class Help
{
	public static List<T> NewList<T>(int capacity, List<T> data = null)
	{
		var list = new List<T>(capacity);
		ZeroList(list, data);
		return list;
	}
	public static void ZeroList<T>(List<T> list, List<T> data)
	{
		if (data != null)
		{
			foreach (var d in data)
				list.Add(d);
		}
		for (int i = list.Count; i < list.Capacity; ++i)
			list.Add(default(T));
	}

	public static List<int> NewUint8Array(int capacity, List<int> data = null)
	{
		return NewList<int>(capacity, data);
	}
	public static List<int> NewUint16Array(int capacity, List<int> data = null)
	{
		return NewList<int>(capacity, data);
	}
	public static List<int> NewUint32Array(int capacity, List<int> data = null)
	{
		return NewList<int>(capacity, data);
	}
}

abstract class Mapper : IMapper
{
	public ICartridge cartridge;
	public List<int> ram;
	public List<int> prg;
	public List<int> chr;

	public Mapper(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr)
	{
		this.cartridge = cartridge;
		this.ram = ram;
		this.prg = prg;
		this.chr = chr;
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper0.ts
//=============
// import { IMapper } from '../api/mapper';
// import {int, int} from '../api/types';
// import { IInterrupt } from '../api/interrupt';
// import { ICartridge } from '../api/cartridge';

// https NROM://wiki.nesdev.com/w/index.php/NROM
class Mapper0 : Mapper
{

	private readonly bool isMirrored;

	public Mapper0(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr) :
		base(cartridge, ram, prg, chr)
	{
		this.isMirrored = prg.Count == 16 * 1024;

		if (chr.Count == 0)
		{
			// If there is no chr memory, treat it as ram
			this.chr = Help.NewUint8Array(0x2000);
		}
	}


	public override int read(int address)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			return this.chr[this.parseAddress(address)];
		}
		else if (address >= 0x8000)
		{
			return this.prg[this.parseAddress(address)];
		}
		else if (address >= 0x6000)
		{
			return this.ram[address - 0x6000];
		}
		else
		{
			// Error TODO handling
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			this.chr[this.parseAddress(address)] = data;
		}
		else if (address >= 0x8000)
		{
			this.prg[this.parseAddress(address)] = data;
		}
		else if (address >= 0x6000)
		{
			this.ram[address - 0x6000] = data;
		}
		else
		{
			// Error TODO handling
		}
	}

	public override void ppuClockHandle(int scanLine, int cycle)
	{
		// Do nothing
	}

	// Refer to http://forums.nesdev.com/viewtopic.php?t=5494
	private int parseAddress(int address)
	{
		if (address < 0x2000)
		{ // CHR
			return address;
		}
		else
		{ // PRG
			return (this.isMirrored ? address & 0xBFF : address) - 0x8000;
		}
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper1.ts
//=============
// import { IMapper } from '../api/mapper';
// import { IInterrupt } from '../api/interrupt';
// import { int, int } from '../api/types';
// import { ICartridge, Mirror } from '../api/cartridge';

// https MMC1://wiki.nesdev.com/w/index.php/MMC1
class Mapper1 : Mapper
{

	private int shiftRegister = 0x10;

	// switch 0 8 KB at a time; switch 1 two separate 4 KB banks
	private int chrBankMode = 0;
	private int[] chrBanks = new int[] { 0, 0 };

	// 0, switch 1 32 KB at $8000, ignoring low bit of bank int
	// fix 2 first bank at $8000 and switch 16 KB bank at $C000
	// fix 3 last bank at $C000 and switch 16 KB bank at $8000
	private int prgBankMode = 0;
	private int prgBanks = 0;
	private int prgBank = 0;

	public Mapper1(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr, int prgBanks = 0) :
		base(cartridge, ram, prg, chr)
	{
		this.prgBanks = prgBanks == 0 ? prg.Count >> 14 : 0;
		this.chr = Help.NewUint8Array(128 * 1024, chr);
		this.prgBankMode = 3;
	}

	public override int read(int address)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			return this.readChr(address);
		}
		else if (address >= 0x8000)
		{
			return this.readPrg(address);
		}
		else if (address >= 0x6000)
		{
			return this.ram[address - 0x6000];
		}
		else
		{
			// Error TODO handling
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			this.writeChr(address, data);
		}
		else if (address >= 0x8000)
		{
			// Load register ($8000-$FFFF)
			this.loadRegister(address, data);
		}
		else if (address >= 0x6000)
		{
			this.ram[address - 0x6000] = data;
		}
		else
		{
			// Error TODO handling
		}
	}

	public override void ppuClockHandle(int scanLine, int cycle)
	{
		// Do nothing
	}

	private void loadRegister(int address, int data)
	{
		if ((data & 0x80) != 0)
		{
			// Clear the shift register
			this.shiftRegister = 0x10;
			this.prgBankMode = 3;
		}
		else
		{
			var isOnFifthWrite = (this.shiftRegister & 0x01) != 0;

			this.shiftRegister >>= 1;
			this.shiftRegister |= (data & 0x01) != 0 ? 0x10 : 0;

			if (isOnFifthWrite)
			{
				this.writeRegister(address, this.shiftRegister);
				this.shiftRegister = 0x10;
			}
		}
	}

	private void writeRegister(int address, int data)
	{
		if (address < 0xA000)
		{
			// Control (internal, $8000-$9FFF)
			switch (data & 0x03)
			{
				case 0:
					this.cartridge.info.mirror = Mirror.SINGLE_SCREEN_LOWER_BANK;
					break;
				case 1:
					this.cartridge.info.mirror = Mirror.SINGLE_SCREEN_UPPER_BANK;
					break;
				case 2:
					this.cartridge.info.mirror = Mirror.VERTICAL;
					break;
				case 3:
					this.cartridge.info.mirror = Mirror.HORIZONTAL;
					break;
			}
			this.prgBankMode = data >> 2 & 0x03;
			this.chrBankMode = data >> 4 & 0x01;
		}
		else if (address < 0xC000)
		{
			// CHR bank 0 (internal, $A000-$BFFF)
			this.chrBanks[0] = data & 0x1F;
		}
		else if (address < 0xE000)
		{
			// CHR bank 1 (internal, $C000-$DFFF)
			this.chrBanks[1] = data & 0x1F;
		}
		else
		{
			// PRG bank (internal, $E000-$FFFF)
			this.prgBank = data & 0x0F;
		}
	}

	private int readChr(int address)
	{
		return this.chr[this.chrOffset(address)];
	}

	private void writeChr(int address, int data)
	{
		this.chr[this.chrOffset(address)] = data;
	}

	private int readPrg(int address)
	{
		return this.prg[this.prgOffset(address)];
	}

	private int chrOffset(int address)
	{
		if (this.chrBankMode != 0)
		{
			// Two separate 4 KB banks
			var bank = address >> 12;
			var offset = address & 0x0FFF;

			return (this.chrBanks[bank] << 12) + offset;
		}
		else
		{
			// 8 KB at a time
			return ((this.chrBanks[0] & 0x1E) << 12) + address;
		}
	}

	private int prgOffset(int address)
	{
		address -= 0x8000;

		var bank = address >> 14;
		var offset = address & 0x3FFF;

		switch (this.prgBankMode)
		{
			case 0:
			case 1:
				// 0, switch 1 32 KB at $8000, ignoring low bit of bank int
				return ((this.prgBank & 0x0E) << 14) + address;
			case 2:
				// fix 2 first bank at $8000 and switch 16 KB bank at $C000
				return bank == 0 ? offset : (this.prgBank << 14) + offset;
			case 3:
				// fix 3 last bank at $C000 and switch 16 KB bank at $8000
				return bank == 0 ? (this.prgBank << 14) + offset : ((this.prgBanks - 1) << 14) + offset;
		}
		return 0;
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper2.ts
//=============
// import { IMapper } from '../api/mapper';
// import { IInterrupt } from '../api/interrupt';
// import { int, int } from '../api/types';
// import { ICartridge } from '../api/cartridge';

// https UxROM://wiki.nesdev.com/w/index.php/UxROM
class Mapper2 : Mapper
{

	private int bankSelect = 0;


	public Mapper2(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr) :
		base(cartridge, ram, prg, chr)
	{
		this.chr = Help.NewUint8Array(8 * 1024, chr);
	}

	public override int read(int address)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			return this.chr[address];
		}
		else if (address >= 0x8000)
		{
			return address < 0xC000 ?
			  // Bank 0
			  this.prg[(this.bankSelect << 14) + address - 0x8000] :
			  // Bank 1
			  this.prg[this.prg.Count - 0x4000 + (address - 0xC000)];
		}
		else if (address >= 0x6000)
		{
			return this.ram[address - 0x6000];
		}
		else
		{
			// Error TODO handling
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			this.chr[address] = data;
		}
		else if (address >= 0x8000)
		{
			// Bank select ($8000-$FFFF)
			this.bankSelect = data & 0x0F;
		}
		else if (address >= 0x6000)
		{
			this.ram[address - 0x6000] = data;
		}
		else
		{
			// Error TODO handling
		}
	}

	public override void ppuClockHandle(int scanLine, int cycle)
	{
		// Do nothing
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper242.ts
//=============
// import { IMapper } from '../api/mapper';
// import { ICartridge, Mirror } from '../api/cartridge';
// import { IInterrupt } from '../api/interrupt';
// import { int, int } from '../api/types';

// INES Mapper https 242://wiki.nesdev.com/w/index.php/INES_Mapper_242
class Mapper242 : Mapper
{

	private int prgBankSelect = 0;

	public Mapper242(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr) :
		base(cartridge, ram, prg, chr)
	{
		this.chr = Help.NewUint8Array(0x2000, chr);
	}

	public override int read(int address)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			return this.chr[address];
		}
		else if (address >= 0x8000)
		{
			return this.prg[(this.prgBankSelect << 15) + address - 0x8000];
		}
		else if (address >= 0x6000)
		{
			return this.ram[address - 0x6000];
		}
		else
		{
			// Error TODO handling
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			this.chr[address] = data;
		}
		else if (address >= 0x8000)
		{
			this.cartridge.info.mirror = (data & 0x02) != 0 ? Mirror.VERTICAL : Mirror.HORIZONTAL;
			this.prgBankSelect = data >> 3 & 0x0F;
		}
		else if (address >= 0x6000)
		{
			this.ram[address - 0x6000] = data;
		}
		else
		{
			// Error TODO handling
		}
	}

	public override void ppuClockHandle(int scanLine, int cycle)
	{
		// Do nothing
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper3.ts
//=============
// import { IMapper } from '../api/mapper';
// import { IInterrupt } from '../api/interrupt';
// import { int, int } from '../api/types';
// import { ICartridge } from '../api/cartridge';

// https CNROM://wiki.nesdev.com/w/index.php/CNROM
class Mapper3 : Mapper
{

	private int chrBankSelect = 0;

	public Mapper3(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr) :
		base(cartridge, ram, prg, chr)
	{
		this.chr = Help.NewUint8Array(32 * 1024, chr);

		this.prg = Help.NewUint8Array(32 * 1024, prg);
		if (prg.Count == 16 * 1024)
		{
			for (int i = 0; i > prg.Count; ++i)      // 镜像
				this.prg[i + prg.Count] = prg[i];
		}
	}

	public override int read(int address)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			return this.chr[(this.chrBankSelect << 13) + address];
		}
		else if (address >= 0x8000)
		{
			return this.prg[address - 0x8000];
		}
		else if (address >= 0x6000)
		{
			return this.ram[address - 0x6000];
		}
		else
		{
			// Error TODO handling
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			this.chr[(this.chrBankSelect << 13) + address] = data;
		}
		else if (address >= 0x8000)
		{
			this.chrBankSelect = data & 0x03;
		}
		else if (address >= 0x6000)
		{
			this.ram[address - 0x6000] = data;
		}
		else
		{
			// Error TODO handling
		}
	}

	public override void ppuClockHandle(int scanLine, int cycle)
	{
		// Do nothing
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper4.ts
//=============
// import { IMapper } from '../api/mapper';
// import { int, int } from '../api/types';
// import { IInterrupt } from '../api/interrupt';
// import { ICartridge, Mirror } from '../api/cartridge';

// https MMC3://wiki.nesdev.com/w/index.php/MMC3
class Mapper4 : Mapper
{

	// https://wiki.nesdev.com/w/index.php/MMC3#CHR_Banks
	// register = ChrBankTable[chrA12Inversion][address >> 10]
	readonly int[,] CHR_BANK_TABLE = new int[,]{
  // CHR A12 inversion is 0
  {0, 0, 1, 1, 2, 3, 4, 5},
  // CHR A12 inversion is 1
  {2, 3, 4, 5, 0, 0, 1, 1},
};

	// https://wiki.nesdev.com/w/index.php/MMC3#PRG_Banks
	// register = PrgBankTable{prgBankMode}{address >> 13}
	readonly int[,] PRG_BANK_TABLE = new int[,]{
  // PRG ROM bank mode is 0
  {6, 7, -2, -1},
  {-2, 7, 6, -1},
};


	private int[] R = new int[8]; // R0 - R7
	private int register = 0; // Index of R
	private int prgBankMode = 0; // 0 or 1
	private int chrA12Inversion = 0; // 0 or 1

	private bool isIrqEnable = false;
	private int irqReloadCounter = 0;
	private int irqCounter = 0;
	protected int prgBanks;

	public Mapper4(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr, int prgBanks = 0) :
		base(cartridge, ram, prg, chr)
	{
		this.prgBanks = prgBanks == 0 ? prg.Count >> 13 : prgBanks;
		this.chr = Help.NewUint8Array(256 * 1024, chr);
	}

	public override int read(int address)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			return this.readChr(address);
		}
		else if (address >= 0x8000)
		{
			return this.readPrg(address);
		}
		else if (address >= 0x6000)
		{
			return this.ram[address - 0x6000];
		}
		else
		{
			// Error TODO handling
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		address &= 0xFFFF;

		if (address < 0x2000)
		{
			this.writeChr(address, data);
		}
		else if (address >= 0x8000)
		{
			this.writeRegister(address, data);
		}
		else if (address >= 0x6000)
		{
			this.ram[address - 0x6000] = data;
		}
		else
		{
			// Error TODO handling
		}
	}

	public override void ppuClockHandle(int scanLine, int cycle)
	{
		if (cycle != 260)
		{
			return;
		}

		if (scanLine > 239 && scanLine < 261)
		{
			return;
		}

		if (this.irqCounter == 0)
		{
			this.irqCounter = this.irqReloadCounter;
		}
		else
		{
			this.irqCounter--;
			if (this.irqCounter == 0 && this.isIrqEnable)
			{
				this.interrupt.irq();
			}
		}
	}

	private int readPrg(int address)
	{
		int addr = this.parsePrgAddress(address);
		var a = this.prg[addr];
		return a;
	}

	private int readChr(int address)
	{
		return this.chr[this.parseChrAddress(address)];
	}

	private void writeChr(int address, int data)
	{
		this.chr[this.parseChrAddress(address)] = data;
	}

	private int parsePrgAddress(int address)
	{
		var cpuBank = (address - 0x8000) >> 13;
		var offset = address & 0x1FFF;

		var register = PRG_BANK_TABLE[this.prgBankMode, cpuBank];
		var bank = register < 0 ? this.prgBanks + register : this.R[register];

		return ((bank << 13) + offset) % this.prg.Count;
	}

	private int parseChrAddress(int address)
	{
		var ppuBank = address >> 10;
		var offset = address & 0x03FF;

		var register = CHR_BANK_TABLE[this.chrA12Inversion, ppuBank];
		var bank = this.R[register];
		if ((register == 0 || register == 1) && (ppuBank % 2) != 0)
		{ // 2KB bank
			bank++;
		}

		return ((bank << 10) + offset) % this.chr.Count;
	}

	private void writeRegister(int address, int data)
	{
		if (address < 0xA000)
		{
			if ((address & 0x01) != 0)
			{
				// Bank data ($8001-$9FFF, odd)
				this.writeBankData(data);
			}
			else
			{ // even
			  // Bank select ($8000-$9FFE, even)
				this.writeBankSelect(data);
			}
		}
		else if (address < 0xC000)
		{
			if ((address & 0x01) != 0)
			{
				// PRG TODO RAM protect ($A001-$BFFF, odd)
			}
			else
			{
				// Mirroring ($A000-$BFFE, even)
				if (this.cartridge.info.mirror != Mirror.FOUR_SCREEN)
				{
					this.cartridge.info.mirror = (data & 0x01) != 0 ? Mirror.HORIZONTAL : Mirror.VERTICAL;
				}
			}
		}
		else if (address < 0xE000)
		{
			if ((address & 0x01) != 0)
			{
				// IRQ reload ($C001-$DFFF, odd)
				this.irqCounter = 0;
			}
			else
			{
				// IRQ latch ($C000-$DFFE, even)
				this.irqReloadCounter = data;
			}
		}
		else
		{
			if ((address & 0x01) != 0)
			{
				// IRQ enable ($E001-$FFFF, odd)
				this.isIrqEnable = true;
			}
			else
			{
				// IRQ disable ($E000-$FFFE, even)
				this.isIrqEnable = false;
			}
		}
	}

	private void writeBankSelect(int data)
	{
		this.register = data & 0x07;
		this.prgBankMode = (data & 0x40) != 0 ? 1 : 0;
		this.chrA12Inversion = (data & 0x80) != 0 ? 1 : 0;
	}

	private void writeBankData(int data)
	{
		if (this.register == 6 || this.register == 7)
		{
			// R6 and R7 will ignore the top two bits
			data &= 0x3F;
		}
		else if (this.register == 0 || this.register == 1)
		{
			// R0 and R1 ignore the bottom bit
			data &= 0xFE;
		}

		this.R[this.register] = data;
	}
}
//=============
// Users C\CGC\Desktop\Python\Test\src\mapper\mapper74.ts
//=============
// import { IMapper } from '../api/mapper';
// import { Mapper4 } from './mapper4';

// INES Mapper https 074://wiki.nesdev.com/w/index.php/INES_Mapper_074
// The circuit board mounts an MMC3 clone together with a 74LS138 and 74LS139 to redirect 1 KiB CHR-ROM banks #8 and #9 to 2 KiB of CHR-RAM.
class Mapper74 : Mapper4
{
	public Mapper74(ICartridge cartridge, List<int> ram, List<int> prg, List<int> chr, int prgBanks = 0) :
		base(cartridge, ram, prg, chr)
	{
		this.prgBanks = prgBanks == 0 ? prg.Count >> 13 : prgBanks;
		this.chr = Help.NewUint8Array(256 * 1024, chr);
	}
}


enum Flags
{
	C = 1 << 0, // Carry
	Z = 1 << 1, // Zero
	I = 1 << 2, // Disable interrupt
	D = 1 << 3, // Decimal Mode ( unused in nes )
	B = 1 << 4, // Break
	U = 1 << 5, // Unused ( always 1 )
	V = 1 << 6, // Overflow
	N = 1 << 7, // Negative
}


internal class IRegisters
{
	public int PC;
	public int SP;
	public int P;
	public int A;
	public int X;
	public int Y;
}

class IOpcodeEntry
{
	public Instruction instruction;
	public AddressingMode addressMode;
	public int bytes;
	public int cycles;
	public int pageCycles;

	public IOpcodeEntry(Instruction i, AddressingMode a, int b, int c, int p)
	{
		instruction = i;
		addressMode = a;
		bytes = b;
		cycles = c;
		pageCycles = p;
	}
};

class Global
{
	public static readonly IOpcodeEntry undefined = new IOpcodeEntry(Instruction.UNDEFINED, AddressingMode.IMPLICIT, 0, 0, 0);

	public static readonly IOpcodeEntry[] OPCODE_TABLE = new IOpcodeEntry[]{
			// http://nesdev.com/the%20%27B%27%20flag%20&%20BRK%20instruction.txt Says:
			//   Regardless of what ANY 6502 documentation says, BRK is a 2 byte opcode. The
			//   first is #$00, and the second is a padding byte. This explains why interrupt
			//   routines called by BRK always return 2 bytes after the actual BRK opcode,
			//   and not just 1.
			// So we use ZERO_PAGE instead of IMPLICIT addressing mode
			new IOpcodeEntry( Instruction.BRK, AddressingMode.ZERO_PAGE, 2, 7, 0 ), // 0

			new IOpcodeEntry( Instruction.ORA, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 1, 1h
			undefined, // 2
			new IOpcodeEntry( Instruction.SLO, AddressingMode.X_INDEXED_INDIRECT, 2, 8, 0 ), // 3, 3h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 4, 4h
			new IOpcodeEntry( Instruction.ORA, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 5, 5h
			new IOpcodeEntry( Instruction.ASL, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 6, 6h
			new IOpcodeEntry( Instruction.SLO, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 7, 7h
			new IOpcodeEntry( Instruction.PHP, AddressingMode.IMPLICIT, 1, 3, 0 ), // 8, 8h
			new IOpcodeEntry( Instruction.ORA, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 9, 9h
			new IOpcodeEntry( Instruction.ASL, AddressingMode.ACCUMULATOR, 1, 2, 0 ), // 10, Ah
			undefined, // 11
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 12, Ch
			new IOpcodeEntry( Instruction.ORA, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 13, Dh
			new IOpcodeEntry( Instruction.ASL, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 14, Eh
			new IOpcodeEntry( Instruction.SLO, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 15, Fh
			new IOpcodeEntry( Instruction.BPL, AddressingMode.RELATIVE, 2, 2, 1 ), // 16, 10h
			new IOpcodeEntry( Instruction.ORA, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 17, 11h
			undefined, // 18
			new IOpcodeEntry( Instruction.SLO, AddressingMode.INDIRECT_Y_INDEXED, 2, 8, 0 ), // 19, 13h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 20, 14h
			new IOpcodeEntry( Instruction.ORA, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 21, 15h
			new IOpcodeEntry( Instruction.ASL, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 22, 16h
			new IOpcodeEntry( Instruction.SLO, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 23, 17h
			new IOpcodeEntry( Instruction.CLC, AddressingMode.IMPLICIT, 1, 2, 0 ), // 24, 18h
			new IOpcodeEntry( Instruction.ORA, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 25, 19h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 26, 1Ah
			new IOpcodeEntry( Instruction.SLO, AddressingMode.ABSOLUTE_Y, 3, 7, 0 ), // 27, 1Bh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 28, 1Ch
			new IOpcodeEntry( Instruction.ORA, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 29, 1Dh
			new IOpcodeEntry( Instruction.ASL, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 30, 1Eh
			new IOpcodeEntry( Instruction.SLO, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 31, 1Fh
			new IOpcodeEntry( Instruction.JSR, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 32, 20h
			new IOpcodeEntry( Instruction.AND, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 33, 21h
			undefined, // 34
			new IOpcodeEntry( Instruction.RLA, AddressingMode.X_INDEXED_INDIRECT, 2, 8, 0 ), // 35, 23h
			new IOpcodeEntry( Instruction.BIT, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 36, 24h
			new IOpcodeEntry( Instruction.AND, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 37, 25h
			new IOpcodeEntry( Instruction.ROL, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 38, 26h
			new IOpcodeEntry( Instruction.RLA, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 39, 27h
			new IOpcodeEntry( Instruction.PLP, AddressingMode.IMPLICIT, 1, 4, 0 ), // 40, 28h
			new IOpcodeEntry( Instruction.AND, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 41, 29h
			new IOpcodeEntry( Instruction.ROL, AddressingMode.ACCUMULATOR, 1, 2, 0 ), // 42, 2Ah
			undefined, // 43
			new IOpcodeEntry( Instruction.BIT, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 44, 2Ch
			new IOpcodeEntry( Instruction.AND, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 45, 2Dh
			new IOpcodeEntry( Instruction.ROL, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 46, 2Eh
			new IOpcodeEntry( Instruction.RLA, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 47, 2Fh
			new IOpcodeEntry( Instruction.BMI, AddressingMode.RELATIVE, 2, 2, 1 ), // 48, 30h
			new IOpcodeEntry( Instruction.AND, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 49, 31h
			undefined, // 50
			new IOpcodeEntry( Instruction.RLA, AddressingMode.INDIRECT_Y_INDEXED, 2, 8, 0 ), // 51, 33h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 52, 34h
			new IOpcodeEntry( Instruction.AND, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 53, 35h
			new IOpcodeEntry( Instruction.ROL, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 54, 36h
			new IOpcodeEntry( Instruction.RLA, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 55, 37h
			new IOpcodeEntry( Instruction.SEC, AddressingMode.IMPLICIT, 1, 2, 0 ), // 56, 38h
			new IOpcodeEntry( Instruction.AND, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 57, 39h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 58, 3Ah
			new IOpcodeEntry( Instruction.RLA, AddressingMode.ABSOLUTE_Y, 3, 7, 0 ), // 59, 3Bh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 60, 3Ch
			new IOpcodeEntry( Instruction.AND, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 61, 3Dh
			new IOpcodeEntry( Instruction.ROL, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 62, 3Eh
			new IOpcodeEntry( Instruction.RLA, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 63, 3Fh
			new IOpcodeEntry( Instruction.RTI, AddressingMode.IMPLICIT, 1, 6, 0 ), // 64, 40h
			new IOpcodeEntry( Instruction.EOR, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 65, 41h
			undefined, // 66
			new IOpcodeEntry( Instruction.SRE, AddressingMode.X_INDEXED_INDIRECT, 2, 8, 0 ), // 67, 43h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 68, 44h
			new IOpcodeEntry( Instruction.EOR, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 69, 45h
			new IOpcodeEntry( Instruction.LSR, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 70, 46h
			new IOpcodeEntry( Instruction.SRE, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 71, 47h
			new IOpcodeEntry( Instruction.PHA, AddressingMode.IMPLICIT, 1, 3, 0 ), // 72, 48H
			new IOpcodeEntry( Instruction.EOR, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 73, 49H
			new IOpcodeEntry( Instruction.LSR, AddressingMode.ACCUMULATOR, 1, 2, 0 ), // 74, 4Ah
			undefined, // 75
			new IOpcodeEntry( Instruction.JMP, AddressingMode.ABSOLUTE, 3, 3, 0 ), // 76, 4Ch
			new IOpcodeEntry( Instruction.EOR, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 77, 4Dh
			new IOpcodeEntry( Instruction.LSR, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 78, 4Eh
			new IOpcodeEntry( Instruction.SRE, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 79, 4Fh
			new IOpcodeEntry( Instruction.BVC, AddressingMode.RELATIVE, 2, 2, 1 ), // 80, 50h
			new IOpcodeEntry( Instruction.EOR, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 81, 51h
			undefined, // 82
			new IOpcodeEntry( Instruction.SRE, AddressingMode.INDIRECT_Y_INDEXED, 2, 8, 0 ), // 83, 53h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 84, 54h
			new IOpcodeEntry( Instruction.EOR, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 85, 55h
			new IOpcodeEntry( Instruction.LSR, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 86, 56h
			new IOpcodeEntry( Instruction.SRE, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 87, 57h
			new IOpcodeEntry( Instruction.CLI, AddressingMode.IMPLICIT, 1, 2, 0 ), // 88, 58h
			new IOpcodeEntry( Instruction.EOR, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 89, 59h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 90, 5Ah
			new IOpcodeEntry( Instruction.SRE, AddressingMode.ABSOLUTE_Y, 3, 7, 0 ), // 91, 5Bh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 92, 5Ch
			new IOpcodeEntry( Instruction.EOR, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 93, 5Dh
			new IOpcodeEntry( Instruction.LSR, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 94, 5Eh
			new IOpcodeEntry( Instruction.SRE, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 95, 5Fh
			new IOpcodeEntry( Instruction.RTS, AddressingMode.IMPLICIT, 1, 6, 0 ), // 96, 60h
			new IOpcodeEntry( Instruction.ADC, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 97, 61h
			undefined, // 98
			new IOpcodeEntry( Instruction.RRA, AddressingMode.X_INDEXED_INDIRECT, 2, 8, 0 ), // 99, 63h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 100, 64h
			new IOpcodeEntry( Instruction.ADC, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 101, 65h
			new IOpcodeEntry( Instruction.ROR, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 102, 66h
			new IOpcodeEntry( Instruction.RRA, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 103, 67h
			new IOpcodeEntry( Instruction.PLA, AddressingMode.IMPLICIT, 1, 4, 0 ), // 104, 68h
			new IOpcodeEntry( Instruction.ADC, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 105, 69h
			new IOpcodeEntry( Instruction.ROR, AddressingMode.ACCUMULATOR, 1, 2, 0 ), // 106, 6Ah
			undefined, // 107
			new IOpcodeEntry( Instruction.JMP, AddressingMode.INDIRECT, 3, 5, 0 ), // 108, 6Ch
			new IOpcodeEntry( Instruction.ADC, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 109, 6Dh
			new IOpcodeEntry( Instruction.ROR, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 110, 6Eh
			new IOpcodeEntry( Instruction.RRA, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 111, 6Fh
			new IOpcodeEntry( Instruction.BVS, AddressingMode.RELATIVE, 2, 2, 1 ), // 112, 70h
			new IOpcodeEntry( Instruction.ADC, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 113, 71h
			undefined, // 114
			new IOpcodeEntry( Instruction.RRA, AddressingMode.INDIRECT_Y_INDEXED, 2, 8, 0 ), // 115, 73h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 116, 74h
			new IOpcodeEntry( Instruction.ADC, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 117, 75h
			new IOpcodeEntry( Instruction.ROR, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 118, 76h
			new IOpcodeEntry( Instruction.RRA, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 119, 77h
			new IOpcodeEntry( Instruction.SEI, AddressingMode.IMPLICIT, 1, 2, 0 ), // 120, 78h
			new IOpcodeEntry( Instruction.ADC, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 121, 79h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 122, 7Ah
			new IOpcodeEntry( Instruction.RRA, AddressingMode.ABSOLUTE_Y, 3, 7, 0 ), // 123, 7Bh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 124, 7Ch
			new IOpcodeEntry( Instruction.ADC, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 125, 7Dh
			new IOpcodeEntry( Instruction.ROR, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 126, 7Eh
			new IOpcodeEntry( Instruction.RRA, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 127, 7Fh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 128, 80h
			new IOpcodeEntry( Instruction.STA, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 129, 81h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 130, 82h
			new IOpcodeEntry( Instruction.SAX, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 131, 83h
			new IOpcodeEntry( Instruction.STY, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 132, 84h
			new IOpcodeEntry( Instruction.STA, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 133, 85h
			new IOpcodeEntry( Instruction.STX, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 134, 86h
			new IOpcodeEntry( Instruction.SAX, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 135, 87h
			new IOpcodeEntry( Instruction.DEY, AddressingMode.IMPLICIT, 1, 2, 0 ), // 136, 88h
			undefined, // 137
			new IOpcodeEntry( Instruction.TXA, AddressingMode.IMPLICIT, 1, 2, 0 ), // 138, 8Ah
			undefined, // 139
			new IOpcodeEntry( Instruction.STY, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 140, 8Ch
			new IOpcodeEntry( Instruction.STA, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 141, 8Dh
			new IOpcodeEntry( Instruction.STX, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 142, 8Eh
			new IOpcodeEntry( Instruction.SAX, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 143, 8Fh
			new IOpcodeEntry( Instruction.BCC, AddressingMode.RELATIVE, 2, 2, 1 ), // 144, 90h
			new IOpcodeEntry( Instruction.STA, AddressingMode.INDIRECT_Y_INDEXED, 2, 6, 0 ), // 145, 91h
			undefined, // 146
			undefined, // 147
			new IOpcodeEntry( Instruction.STY, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 148, 94h
			new IOpcodeEntry( Instruction.STA, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 149, 95h
			new IOpcodeEntry( Instruction.STX, AddressingMode.ZERO_PAGE_Y, 2, 4, 0 ), // 150, 96h
			new IOpcodeEntry( Instruction.SAX, AddressingMode.ZERO_PAGE_Y, 2, 4, 0 ), // 151, 97h
			new IOpcodeEntry( Instruction.TYA, AddressingMode.IMPLICIT, 1, 2, 0 ), // 152, 98h
			new IOpcodeEntry( Instruction.STA, AddressingMode.ABSOLUTE_Y, 3, 5, 0 ), // 153, 99h
			new IOpcodeEntry( Instruction.TXS, AddressingMode.IMPLICIT, 1, 2, 0 ), // 154, 9Ah
			undefined, // 155
			undefined, // 156
			new IOpcodeEntry( Instruction.STA, AddressingMode.ABSOLUTE_X, 3, 5, 0 ), // 157, 9Dh
			undefined, // 158
			undefined, // 159
			new IOpcodeEntry( Instruction.LDY, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 160, A0h
			new IOpcodeEntry( Instruction.LDA, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 161, A1h
			new IOpcodeEntry( Instruction.LDX, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 162, A2h
			new IOpcodeEntry( Instruction.LAX, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 163, A3h
			new IOpcodeEntry( Instruction.LDY, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 164, A4h
			new IOpcodeEntry( Instruction.LDA, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 165, A5h
			new IOpcodeEntry( Instruction.LDX, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 166, A6h
			new IOpcodeEntry( Instruction.LAX, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 167, A7h
			new IOpcodeEntry( Instruction.TAY, AddressingMode.IMPLICIT, 1, 2, 0 ), // 168, A8h
			new IOpcodeEntry( Instruction.LDA, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 169, A9h
			new IOpcodeEntry( Instruction.TAX, AddressingMode.IMPLICIT, 1, 2, 0 ), // 170, AAh
			undefined, // 171
			new IOpcodeEntry( Instruction.LDY, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 172, ACh
			new IOpcodeEntry( Instruction.LDA, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 173, ADh
			new IOpcodeEntry( Instruction.LDX, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 174, AEh
			new IOpcodeEntry( Instruction.LAX, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 175, AFh
			new IOpcodeEntry( Instruction.BCS, AddressingMode.RELATIVE, 2, 2, 1 ), // 176, B0h
			new IOpcodeEntry( Instruction.LDA, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 177, B1h
			undefined, // 178
			new IOpcodeEntry( Instruction.LAX, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 179, B3h
			new IOpcodeEntry( Instruction.LDY, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 180, B4h
			new IOpcodeEntry( Instruction.LDA, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 181, B5h
			new IOpcodeEntry( Instruction.LDX, AddressingMode.ZERO_PAGE_Y, 2, 4, 0 ), // 182, B6h
			new IOpcodeEntry( Instruction.LAX, AddressingMode.ZERO_PAGE_Y, 2, 4, 0 ), // 183, B7h
			new IOpcodeEntry( Instruction.CLV, AddressingMode.IMPLICIT, 1, 2, 0 ), // 184, B8h
			new IOpcodeEntry( Instruction.LDA, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 185, B9h
			new IOpcodeEntry( Instruction.TSX, AddressingMode.IMPLICIT, 1, 2, 0 ), // 186, BAh
			undefined, // 187
			new IOpcodeEntry( Instruction.LDY, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 188, BCh
			new IOpcodeEntry( Instruction.LDA, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 189, BDh
			new IOpcodeEntry( Instruction.LDX, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 190, BEh
			new IOpcodeEntry( Instruction.LAX, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 191, BFh
			new IOpcodeEntry( Instruction.CPY, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 192, C0h
			new IOpcodeEntry( Instruction.CMP, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 193, C1h
			undefined, // 194
			new IOpcodeEntry( Instruction.DCP, AddressingMode.X_INDEXED_INDIRECT, 2, 8, 0 ), // 195, C3h
			new IOpcodeEntry( Instruction.CPY, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 196, C4h
			new IOpcodeEntry( Instruction.CMP, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 197, C5h
			new IOpcodeEntry( Instruction.DEC, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 198, C6h
			new IOpcodeEntry( Instruction.DCP, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 199, C7h
			new IOpcodeEntry( Instruction.INY, AddressingMode.IMPLICIT, 1, 2, 0 ), // 200, C8h
			new IOpcodeEntry( Instruction.CMP, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 201, C9h
			new IOpcodeEntry( Instruction.DEX, AddressingMode.IMPLICIT, 1, 2, 0 ), // 202, CAh
			undefined, // 203
			new IOpcodeEntry( Instruction.CPY, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 204, CCh
			new IOpcodeEntry( Instruction.CMP, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 205, CDh
			new IOpcodeEntry( Instruction.DEC, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 206, CEh
			new IOpcodeEntry( Instruction.DCP, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 207, CFh
			new IOpcodeEntry( Instruction.BNE, AddressingMode.RELATIVE, 2, 2, 1 ), // 208, D0h
			new IOpcodeEntry( Instruction.CMP, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 209, D1h
			undefined, // 210
			new IOpcodeEntry( Instruction.DCP, AddressingMode.INDIRECT_Y_INDEXED, 2, 8, 0 ), // 211, D3h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 212, D4h
			new IOpcodeEntry( Instruction.CMP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 213, D5h
			new IOpcodeEntry( Instruction.DEC, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 214, D6h
			new IOpcodeEntry( Instruction.DCP, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 215, D7h
			new IOpcodeEntry( Instruction.CLD, AddressingMode.IMPLICIT, 1, 2, 0 ), // 216, D8h
			new IOpcodeEntry( Instruction.CMP, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 217, D9h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 218, DAh
			new IOpcodeEntry( Instruction.DCP, AddressingMode.ABSOLUTE_Y, 3, 7, 0 ), // 219, DBh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 220, DCh
			new IOpcodeEntry( Instruction.CMP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 221, DDh
			new IOpcodeEntry( Instruction.DEC, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 222, DEh
			new IOpcodeEntry( Instruction.DCP, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 223, DFh
			new IOpcodeEntry( Instruction.CPX, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 224, E0h
			new IOpcodeEntry( Instruction.SBC, AddressingMode.X_INDEXED_INDIRECT, 2, 6, 0 ), // 225, E1h
			undefined, // 226
			new IOpcodeEntry( Instruction.ISC, AddressingMode.X_INDEXED_INDIRECT, 2, 8, 0 ), // 227, E3h
			new IOpcodeEntry( Instruction.CPX, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 228, E4h
			new IOpcodeEntry( Instruction.SBC, AddressingMode.ZERO_PAGE, 2, 3, 0 ), // 229, E5h
			new IOpcodeEntry( Instruction.INC, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 230, E6h
			new IOpcodeEntry( Instruction.ISC, AddressingMode.ZERO_PAGE, 2, 5, 0 ), // 231, E7h
			new IOpcodeEntry( Instruction.INX, AddressingMode.IMPLICIT, 1, 2, 0 ), // 232, E8h
			new IOpcodeEntry( Instruction.SBC, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 233, E9h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 234, EAh
			new IOpcodeEntry( Instruction.SBC, AddressingMode.IMMEDIATE, 2, 2, 0 ), // 235, EBh
			new IOpcodeEntry( Instruction.CPX, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 236, ECh
			new IOpcodeEntry( Instruction.SBC, AddressingMode.ABSOLUTE, 3, 4, 0 ), // 237, EDh
			new IOpcodeEntry( Instruction.INC, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 238, EEh
			new IOpcodeEntry( Instruction.ISC, AddressingMode.ABSOLUTE, 3, 6, 0 ), // 239, EFh
			new IOpcodeEntry( Instruction.BEQ, AddressingMode.RELATIVE, 2, 2, 1 ), // 240, F0h
			new IOpcodeEntry( Instruction.SBC, AddressingMode.INDIRECT_Y_INDEXED, 2, 5, 1 ), // 241, F1h
			undefined, // 242
			new IOpcodeEntry( Instruction.ISC, AddressingMode.INDIRECT_Y_INDEXED, 2, 8, 0 ), // 243, F3h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 244, F4h
			new IOpcodeEntry( Instruction.SBC, AddressingMode.ZERO_PAGE_X, 2, 4, 0 ), // 245, F5h
			new IOpcodeEntry( Instruction.INC, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 246, F6h
			new IOpcodeEntry( Instruction.ISC, AddressingMode.ZERO_PAGE_X, 2, 6, 0 ), // 247, F7h
			new IOpcodeEntry( Instruction.SED, AddressingMode.IMPLICIT, 1, 2, 0 ), // 248, F8h
			new IOpcodeEntry( Instruction.SBC, AddressingMode.ABSOLUTE_Y, 3, 4, 1 ), // 249, F9h
			new IOpcodeEntry( Instruction.NOP, AddressingMode.IMPLICIT, 1, 2, 0 ), // 250, FAh
			new IOpcodeEntry( Instruction.ISC, AddressingMode.ABSOLUTE_Y, 3, 7, 0 ), // 251, FBh
			new IOpcodeEntry( Instruction.NOP, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 252, FCh
			new IOpcodeEntry( Instruction.SBC, AddressingMode.ABSOLUTE_X, 3, 4, 1 ), // 253, FDh
			new IOpcodeEntry( Instruction.INC, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 254, FEh
			new IOpcodeEntry( Instruction.ISC, AddressingMode.ABSOLUTE_X, 3, 7, 0 ), // 255, FFh
		};
	public static readonly int NaN = int.MaxValue;

	public static bool isNaN(int v)
	{
		return v == NaN;
	}


	public static readonly int[] BaseNameTableAddressList = { 0x2000, 0x2400, 0x2800, 0x2C00 };

	public static readonly int[] LENGTH_TABLE = {
			10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
			12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30,
		};

	public static readonly int[] NOISE_PEROID_TABLE = {
			4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068,
		};

	public static readonly int[,] DUTY_TABLE = {
			{ 0, 0, 0, 0, 0, 0, 0, 1 },
			{ 0, 0, 0, 0, 0, 0, 1, 1 },
			{ 0, 0, 0, 0, 1, 1, 1, 1 },
			{ 1, 1, 1, 1, 1, 1, 0, 0 },
		};

	public static readonly int[] TRIANGLE_VOLUME_TABLE = {
			15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
		};

	public static readonly int[] DMC_TABLE = {
			428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54,
		};

	public static readonly UInt32[] TABLE = {
			0x808080, 0x0000BB, 0x3700BF, 0x8400A6,
			0xBB006A, 0xB7001E, 0xB30000, 0x912600,
			0x7B2B00, 0x003E00, 0x00480D, 0x003C22,
			0x002F66, 0x000000, 0x050505, 0x050505,

			0xC8C8C8, 0x0059FF, 0x443CFF, 0xB733CC,
			0xFF33AA, 0xFF375E, 0xFF371A, 0xD54B00,
			0xC46200, 0x3C7B00, 0x1E8415, 0x009566,
			0x0084C4, 0x111111, 0x090909, 0x090909,

			0xFFFFFF, 0x0095FF, 0x6F84FF, 0xD56FFF,
			0xFF77CC, 0xFF6F99, 0xFF7B59, 0xFF915F,
			0xFFA233, 0xA6BF00, 0x51D96A, 0x4DD5AE,
			0x00D9FF, 0x666666, 0x0D0D0D, 0x0D0D0D,

			0xFFFFFF, 0x84BFFF, 0xBBBBFF, 0xD0BBFF,
			0xFFBFEA, 0xFFBFCC, 0xFFC4B7, 0xFFCCAE,
			0xFFD9A2, 0xCCE199, 0xAEEEB7, 0xAAF7EE,
			0xB3EEFF, 0xDDDDDD, 0x111111, 0x111111,
		 };

	public static double NextDouble()
	{
		Random r = new Random();
		return r.NextDouble();
	}

}

abstract class Channel : IChannel
{
	protected int timer = 0; // 11bit
	protected int internalTimer = 0;

	public override void clock()
	{
		if (!this.enable)
			return;

		if (this.internalTimer == 0)
		{
			this.internalTimer = this.timer;
			this.step();
		}
		else
		{
			this.internalTimer--;
		}
	}

	protected virtual void step() { }
};

class Pulse : Channel
{
	//volume = 0; // 0-15
	//lengthCounter = 0; // 5bit


	private int duty = 0; // 2bit
	private bool isEnvelopeLoop = false;
	private bool isConstantVolume = false;
	private int envelopeValue = 0; // 4bit
	private int envelopeVolume = 0; // 4bit
	private int envelopeCounter = 0;
	private bool isSweepEnabled = false;
	private int sweepPeriod = 0; // 3bit
	private bool isSweepNegated = false;
	private int sweepShift = 0; // 3bit
	private int sweepCounter = 0;
	private int counter = 0;
	private int channel;


	public Pulse(int channel1)
	{
		channel = channel1;
	}

	public override void processEnvelope()
	{
		if (this.isConstantVolume)
		{
			return;
		}

		if (this.envelopeCounter % (this.envelopeValue + 1) == 0)
		{
			if (this.envelopeVolume == 0)
			{
				this.envelopeVolume = this.isEnvelopeLoop ? 15 : 0;
			}
			else
			{
				this.envelopeVolume--;
			}
		}

		this.envelopeCounter++;
	}


	public override void processLengthCounter()
	{
		if (!this.isEnvelopeLoop && this.lengthCounter > 0)
		{
			this.lengthCounter--;
		}
	}

	public override void processSweep()
	{
		if (!this.isSweepEnabled)
		{
			return;
		}

		if (this.sweepCounter % (this.sweepPeriod + 1) == 0)
		{
			// 1. A barrel shifter shifts the channel's 11-bit raw timer period right by the shift count, producing the change amount.
			// 2. If the negate flag is true, the change amount is made negative.
			// 3. The target period is the sum of the current period and the change amount.
			int changeAmount = this.isSweepNegated ? -(this.timer >> this.sweepShift) : this.timer >> this.sweepShift;
			this.timer += changeAmount;

			// The two pulse channels have their adders' carry inputs wired differently,
			// which produces different results when each channel's change amount is made negative:
			//   - Pulse 1 adds the ones' complement (−c − 1). Making 20 negative produces a change amount of −21.
			//   - Pulse 2 adds the two's complement (−c). Making 20 negative produces a change amount of −20.
			if (this.channel == 1 && changeAmount <= 0)
			{
				this.timer--;
			}
		}

		this.sweepCounter++;
	}

	public override void write(int offset, int data)
	{
		switch (offset)
		{
			case 0:
				this.duty = data >> 6;
				this.isEnvelopeLoop = (data & 0x20) != 0;
				this.isConstantVolume = (data & 0x10) != 0;
				this.envelopeValue = data & 0x0F;

				this.envelopeVolume = 15;
				this.envelopeCounter = 0;
				break;
			case 1:
				this.isSweepEnabled = (data & 0x80) != 0;
				this.sweepPeriod = data >> 4 & 0x07;
				this.isSweepNegated = (data & 0x08) != 0;
				this.sweepShift = data & 0x07;

				this.sweepCounter = 0;
				break;
			case 2:
				this.timer = this.timer & 0xFF00 | data;
				break;
			case 3:
				this.timer = this.timer & 0x00FF | (data << 8) & 0x07FF;
				this.lengthCounter = Global.LENGTH_TABLE[data >> 3];

				this.internalTimer = 0;
				break;
		}
	}

	protected override void step()
	{
		this.counter++;

		// If at any time the target period is greater than $7FF, the sweep unit mutes the channel
		// If the current period is less than 8, the sweep unit mutes the channel
		if (!this.enable || this.lengthCounter == 0 || this.timer < 8 || this.timer > 0x7FF)
		{
			this.volume = 0;
		}
		else if (this.isConstantVolume)
		{
			this.volume = this.envelopeValue * Global.DUTY_TABLE[this.duty, this.counter & 0x07];
		}
		else
		{
			this.volume = this.envelopeVolume * Global.DUTY_TABLE[this.duty, this.counter & 0x07];
		}
	}
}

class Triangle : Channel
{
	//volume = 0; // 0-15
	//lengthCounter = 0; // 5bit

	bool lenghtCounterHalt = false;
	int linearCounterLoad = 0; // 7bit
	bool linearCounterReloadFlag = false;
	int linearCounterValue = 0;
	int counter = 0;


	public override void processLinearCounter()
	{
		// When the frame counter generates a linear counter clock, the following actions occur in order:
		//   - If the linear counter reload flag is set, the linear counter is reloaded with the counter reload value,
		//     otherwise if the linear counter is non-zero, it is decremented.
		//   - If the control flag is clear, the linear counter reload flag is cleared.
		if (this.linearCounterReloadFlag)
		{
			this.linearCounterValue = this.linearCounterLoad;
		}
		else if (this.linearCounterValue > 0)
		{
			this.linearCounterValue--;
		}

		if (!this.lenghtCounterHalt)
		{
			this.linearCounterReloadFlag = false;
		}
	}

	public override void processLengthCounter()
	{
		if (!this.lenghtCounterHalt && this.lengthCounter > 0)
		{
			this.lengthCounter--;
		}
	}


	public override void write(int offset, int data)
	{
		switch (offset)
		{
			case 0:
				this.lenghtCounterHalt = (data & 0x80) != 0;
				this.linearCounterLoad = data & 0x7F;
				break;
			case 1:
				break;
			case 2:
				this.timer = this.timer & 0xFF00 | data;
				break;
			case 3:
				this.timer = this.timer & 0x00FF | (data << 8) & 0x07FF;
				this.lengthCounter = Global.LENGTH_TABLE[data >> 3];

				this.linearCounterReloadFlag = true;
				this.internalTimer = 0;
				break;
		}
	}

	protected override void step()
	{
		this.counter++;

		if (!this.enable || this.lengthCounter == 0 || this.linearCounterValue == 0)
		{
			// Eliminate popping noise
			this.counter--;
			this.volume = Global.TRIANGLE_VOLUME_TABLE[this.counter & 0x1F];
		}
		else
		{
			this.volume = Global.TRIANGLE_VOLUME_TABLE[this.counter & 0x1F];
		}
	}
}


// timer 原来是 noisePeriod
class Noise : Channel
{
	//volume = 0; // 4bit
	//lengthCounter = 0;

	bool isLengthCounterHalt = false;
	bool isConstantVolume = false;
	int envelopeValue = 0;
	int envelopeVolume = 0;
	int envelopeCounter = 0;

	bool isLoopNoise = false;

	public override void processEnvelope()
	{
		if (this.isConstantVolume)
		{
			return;
		}

		if (this.envelopeCounter % (this.envelopeValue + 1) == 0)
		{
			if (this.envelopeVolume == 0)
			{
				this.envelopeVolume = this.isLengthCounterHalt ? 15 : 0;
			}
			else
			{
				this.envelopeVolume--;
			}
		}

		this.envelopeCounter++;
	}


	public override void processLengthCounter()
	{
		if (!this.isLengthCounterHalt && this.lengthCounter > 0)
		{
			this.lengthCounter--;
		}
	}


	public override void write(int offset, int data)
	{
		switch (offset)
		{
			case 0:
				this.isLengthCounterHalt = (data & 0x20) != 0;
				this.isConstantVolume = (data & 0x10) != 0;
				this.envelopeValue = data & 0x0F;

				this.envelopeVolume = 15;
				this.envelopeCounter = 0;
				break;
			case 1:
				break;
			case 2:
				this.isLoopNoise = (data & 0x80) != 0;
				/*this.noisePeriod = NOISE_PEROID_TABLE[data & 0x0F];*/
				this.timer = Global.NOISE_PEROID_TABLE[data & 0x0F];

				this.internalTimer = 0;
				break;
			case 3:
				this.lengthCounter = Global.LENGTH_TABLE[data >> 3];
				break;
		}
	}

	protected override void step()
	{
		if (!this.enable || this.lengthCounter == 0)
		{
			this.volume = 0;
		}
		else if (this.isConstantVolume)
		{
			this.volume = (int)Math.Floor(Global.NextDouble() * this.envelopeValue);
		}
		else
		{
			this.volume = (int)Math.Floor(Global.NextDouble() * this.envelopeVolume);
		}
	}
}

// APU DMC: http://wiki.nesdev.com/w/index.php/APU_DMC
class Dmc
{
	public int volume = 0; // 7bit
	public bool isEnabled = false;
	public IBus cpuBus;
	public int bytesRemainingCounter = 0;
	public IInterrupt interrupt;
	public bool interruptFlag = false;

	bool isMuted = true;

	bool isIrqEnabled = false;
	bool isLoopEnabled = false;
	int frequency = 0; // 4bit
	int loadCounter = 0; // 7bit
	int sampleAddress = 0;
	int sampleLength = 0;

	int clocks = 0;

	int sampleBuffer = 0;
	int addressCounter = 0;

	int bitsRemainingCounter = 0;

	public void clock()
	{
		if (!this.isEnabled)
		{
			return;
		}

		if (this.clocks % (Global.DMC_TABLE[this.frequency] + 1) == 0)
		{
			this.outputUnit();
		}

		this.clocks++;
	}

	public void write(int offset, int data)
	{
		switch (offset)
		{
			case 0:
				this.isIrqEnabled = (data & 0x80) != 0;
				this.isLoopEnabled = (data & 0x40) != 0;
				this.frequency = data & 0x0F;
				this.clocks = 0;

				// If clear IRQ flag, the interrupt flag is cleared.
				if (!this.isIrqEnabled)
				{
					this.interruptFlag = false;
				}
				break;
			case 1:
				this.loadCounter = data & 0x7F;

				this.restartSample();
				break;
			case 2:
				// Sample address = %11AAAAAA.AA000000 = $C000 + (A * 64)
				this.sampleAddress = 0xC000 + data * 64;

				this.restartSample();
				break;
			case 3:
				// Sample length = %LLLL.LLLL0001 = (L * 16) + 1 bytes
				this.sampleLength = data * 16 + 1;

				this.restartSample();
				break;
		}
	}

	void restartSample()
	{
		// When a sample is (re)started, the current address is set to the sample address, and bytes remaining is set to the sample length.
		this.addressCounter = this.sampleAddress;
		this.bytesRemainingCounter = this.sampleLength;
		this.isMuted = false;
		this.volume = this.loadCounter;
	}

	// http://wiki.nesdev.com/w/index.php/APU_DMC#Memory_reader
	void memoryReader()
	{
		// When the sample buffer is emptied, the memory reader fills the sample buffer with the next byte from the currently playing sample.
		// It has an address counter and a bytes remaining counter.

		if (this.bytesRemainingCounter <= 0 || this.bitsRemainingCounter > 0)
		{
			return;
		}

		// TODO: The CPU is stalled for up to 4 CPU cycles to allow the longest possible write

		// The sample buffer is filled with the next sample byte read from the current address
		this.sampleBuffer = this.cpuBus.readByte(this.addressCounter);

		// The address is incremented; if it exceeds $FFFF, it is wrapped around to $8000.
		this.addressCounter = this.addressCounter >= 0xFFFF ? 0x8000 : this.addressCounter + 1;

		// The bytes remaining counter is decremented; if it becomes zero and the loop flag is set, the sample is restarted (see above);
		// otherwise, if the bytes remaining counter becomes zero and the IRQ enabled flag is set, the interrupt flag is set.
		this.bytesRemainingCounter--;
		if (this.bytesRemainingCounter <= 0)
		{
			if (this.isLoopEnabled)
			{
				this.restartSample();
			}
			else
			{
				this.isMuted = true;

				if (this.isIrqEnabled)
				{
					this.interruptFlag = true;
					this.interrupt.irq();
				}
			}
		}
	}

	// http://wiki.nesdev.com/w/index.php/APU_DMC#Output_unit
	void outputUnit()
	{
		if (this.bitsRemainingCounter <= 0)
		{
			if (this.isMuted)
			{
				return;
			}

			this.memoryReader();
			this.bitsRemainingCounter = 8;
		}

		// If the silence flag is clear, the output level changes based on bit 0 of the shift _register. If the bit is 1, add 2; otherwise, subtract 2.
		// But if adding or subtracting 2 would cause the output level to leave the 0-127 range, leave the output level unchanged.
		// This means subtract 2 only if the current level is at least 2, or add 2 only if the current level is at most 125.
		if ((this.sampleBuffer & 0x01) != 0)
		{
			this.volume = this.volume > 125 ? 127 : this.volume + 2;
		}
		else
		{
			this.volume = this.volume < 2 ? 0 : this.volume - 2;
		}

		this.sampleBuffer >>= 1;
		this.bitsRemainingCounter--;
	}
}


class APU : IAPU
{
	public IInterrupt interruptLine;

	Pulse pulse1;
	Pulse pulse2;
	Triangle triangle;
	Noise noise;
	Dmc dmc = new Dmc();

	bool frameInterruptFlag = false;

	// mode 0:    mode 1:       function
	// ---------  -----------  -----------------------------
	//  - - - f    - - - - -    IRQ (if bit 6 is clear)
	//  - l - l    - l - - l    Length counter and sweep
	//  e e e e    e e e - e    Envelope and linear counter
	int mode = 0;
	bool isIRQEnabled = true;

	int clocks = 0;
	int sampleCounter = 0;
	int frameCounter = 0;

	int sampleRate = 48000;
	OnSample onSample;

	public APU(int sampleRate1, OnSample onSample1)
	{
		sampleRate = sampleRate1;
		onSample = onSample1;
		pulse1 = new Pulse(1);
		pulse2 = new Pulse(2);
		triangle = new Triangle();
		noise = new Noise();
	}

	public void SetCpuBus(IBus value)
	{
		this.dmc.cpuBus = value;
	}

	public void SetInterrupt(IInterrupt value)
	{
		this.interruptLine = value;
		this.dmc.interrupt = value;
	}

	public override void clock()
	{
		this.clocks++;

		if ((this.clocks & 0x01) != 0)
		{
			this.pulse1.clock();
			this.pulse2.clock();
			this.noise.clock();
		}
		this.dmc.clock();
		this.triangle.clock();

		int count = (int)Math.Floor((double)this.clocks / ((double)1789773 / this.sampleRate));
		if (count != this.sampleCounter)
		{
			this.sampleCounter = count;
			this.sampleOutput();
		}

		int frameCount = (int)Math.Floor((double)this.clocks / ((double)1789773 / 240));
		if (frameCount != this.frameCounter)
		{
			this.frameCounter = frameCount;
			this.processFrameCounter();
		}
	}


	public override int read(int address)
	{
		if (address == 0x4015)
		{
			int data = (this.pulse1.lengthCounter > 0 ? 0x01 : 0) |
				(this.pulse2.lengthCounter > 0 ? 0x02 : 0) |
				(this.triangle.lengthCounter > 0 ? 0x04 : 0) |
				(this.noise.lengthCounter > 0 ? 0x08 : 0) |
				(this.dmc.bytesRemainingCounter > 0 ? 0x10 : 0) |
				(this.frameInterruptFlag ? 0x40 : 0) |
				(this.dmc.interruptFlag ? 0x80 : 0);

			// Reading this _register clears the frame interrupt flag (but not the DMC interrupt flag).
			this.frameInterruptFlag = false;

			// TODO: If an interrupt flag was set at the same moment of the read, it will read back as 1 but it will not be cleared.

			return data;
		}
		else
		{
			return 0;
		}
	}

	public override void write(int address, int data)
	{
		switch (address)
		{
			case 0x4000:
			case 0x4001:
			case 0x4002:
			case 0x4003:
				this.pulse1.write(address - 0x4000, data);
				break;
			case 0x4004:
			case 0x4005:
			case 0x4006:
			case 0x4007:
				this.pulse2.write(address - 0x4004, data);
				break;
			case 0x4008:
			case 0x4009:
			case 0x400A:
			case 0x400B:
				this.triangle.write(address - 0x4008, data);
				break;
			case 0x400C:
			case 0x400D:
			case 0x400E:
			case 0x400F:
				this.noise.write(address - 0x400C, data);
				break;
			case 0x4010:
			case 0x4011:
			case 0x4012:
			case 0x4013:
				this.dmc.write(address - 0x4010, data);
				break;
			case 0x4015:
				this.pulse1.enable = (data & 0x01) != 0;
				this.pulse2.enable = (data & 0x02) != 0;
				this.triangle.enable = (data & 0x04) != 0;
				this.noise.enable = (data & 0x08) != 0;
				this.dmc.isEnabled = (data & 0x10) != 0;

				// Writing to this _register clears the DMC interrupt flag.
				this.dmc.interruptFlag = false;
				break;
			case 0x4017:
				this.frameCounter = 0;
				this.mode = data >> 7;
				this.isIRQEnabled = (data & 0x40) == 0;
				break;
		}
	}

	// http://wiki.nesdev.com/w/index.php/APU_Mixer
	void sampleOutput()
	{
		float pulseOut = 0.00752f * (this.pulse1.volume + this.pulse2.volume);
		float tndOut = 0.00851f * this.triangle.volume + 0.00494f * this.noise.volume + 0.00335f * this.dmc.volume;

		this.onSample(pulseOut + tndOut);
	}

	void processFrameCounter()
	{
		if (this.mode == 0)
		{ // 4 Step mode
			switch (this.frameCounter % 4)
			{
				case 0:
					this.processEnvelopeAndLinearCounter();
					break;
				case 1:
					this.processLengthCounterAndSweep();
					this.processEnvelopeAndLinearCounter();
					break;
				case 2:
					this.processEnvelopeAndLinearCounter();
					break;
				case 3:
					this.triggerIRQ();
					this.processLengthCounterAndSweep();
					this.processEnvelopeAndLinearCounter();
					break;
			}
		}
		else
		{ // 5 Step mode
			switch (this.frameCounter % 5)
			{
				case 0:
					this.processEnvelopeAndLinearCounter();
					break;
				case 1:
					this.processLengthCounterAndSweep();
					this.processEnvelopeAndLinearCounter();
					break;
				case 2:
					this.processEnvelopeAndLinearCounter();
					break;
				case 3:
					break;
				case 4:
					this.processLengthCounterAndSweep();
					this.processEnvelopeAndLinearCounter();
					break;
			}
		}
	}

	void processEnvelopeAndLinearCounter()
	{
		this.pulse1.processEnvelope();
		this.pulse2.processEnvelope();
		this.noise.processEnvelope();

		this.triangle.processLinearCounter();
	}

	void processLengthCounterAndSweep()
	{
		this.pulse1.processLengthCounter();
		this.pulse2.processLengthCounter();
		this.triangle.processLengthCounter();
		this.noise.processLengthCounter();

		this.pulse1.processSweep();
		this.pulse2.processSweep();
	}


	void triggerIRQ()
	{
		if (!this.isIRQEnabled)
		{
			return;
		}

		this.frameInterruptFlag = true;
		this.interruptLine.irq();
	}
};


class DMA : IDMA
{
	public ICPU cpu;
	public IPPU ppu;


	public void copy(int cpuBusAddr)
	{
		List<int> data = new List<int>(256);
		for (int i = 0; i < 256; i++)
		{
			var value = cpu.GetBus().readByte(cpuBusAddr + i);
			data.Add(value);
		}

		this.ppu.dmaCopy(data);

		// The CPU is suspended during the transfer, which will take 513 or 514 cycles after the $4014 write tick.
		// (1 dummy read cycle while waiting for writes to complete, +1 if on an odd CPU cycle, then 256 alternating read/write cycles.)

		//@@ (this.cpu as any).suspendCycles = (this.cpu as any).cycles & 0x01 ? 513 : 514;
	}
};

class Interrupt : IInterrupt
{
	public ICPU cpu;
	public void irq()
	{
		this.cpu.irq();
	}

	public void nmi()
	{
		this.cpu.nmi();
	}
};


class PPUController : IPPUController
{
	public PPUController()
	{
		baseNameTableAddress = Global.BaseNameTableAddressList[0];
		vramIncrementStepSize = 1;
		spritePatternTableAddress = 0;
		backgroundPatternTableAddress = 0;
		spriteSize = SpriteSize.SIZE_8X8;
		isNMIEnabled = false;
	}

	int IndexOf(int value)
	{
		for (int i = 0; i < 4; ++i)
		{
			if (Global.BaseNameTableAddressList[i] == value)
				return i;
		}
		return -1;
	}

	public override int data
	{
		get
		{
			return IndexOf(this.baseNameTableAddress) |
				(this.vramIncrementStepSize == 1 ? 0 : 1) << 2 |
				(this.spritePatternTableAddress != 0 ? 1 : 0) << 3 |
				(this.backgroundPatternTableAddress != 0 ? 1 : 0) << 4 |
				(this.spriteSize == SpriteSize.SIZE_8X8 ? 0 : 1) << 5 |
				(this.isNMIEnabled ? 1 : 0) << 7;
		}
		set
		{
			this.baseNameTableAddress = Global.BaseNameTableAddressList[value & 0x03];
			this.vramIncrementStepSize = (value & 0x04) != 0 ? 32 : 1;
			this.spritePatternTableAddress = (value & 0x08) != 0 ? 0x1000 : 0;
			this.backgroundPatternTableAddress = (value & 0x10) != 0 ? 0x1000 : 0;
			this.spriteSize = (value & 0x20) != 0 ? SpriteSize.SIZE_8X16 : SpriteSize.SIZE_8X8;
			this.isNMIEnabled = (value & 0x80) != 0;

		}
	}

};

class Mask : IMask
{
	public override int data
	{
		get
		{
			return (this.isColorful ? 0 : 1) |
				(this.isShowBackgroundLeft8px ? 1 : 0) << 1 |
				(this.isShowSpriteLeft8px ? 1 : 0) << 2 |
				(this.isShowBackground ? 1 : 0) << 3 |
				(this.isShowSprite ? 1 : 0) << 4 |
				(this.isEmphasizeRed ? 1 : 0) << 5 |
				(this.isEmphasizeGreen ? 1 : 0) << 6 |
				(this.isEmphasizeBlue ? 1 : 0) << 7;
		}
		set
		{
			this.isColorful = (value & 0x01) == 0;
			this.isShowBackgroundLeft8px = (value & 0x02) != 0;
			this.isShowSpriteLeft8px = (value & 0x04) != 0;
			this.isShowBackground = (value & 0x08) != 0;
			this.isShowSprite = (value & 0x10) != 0;
			this.isEmphasizeRed = (value & 0x20) != 0;
			this.isEmphasizeGreen = (value & 0x40) != 0;
			this.isEmphasizeBlue = (value & 0x80) != 0;
		}
	}
};

enum Register
{
	PPUCTRL = 0x2000, // RW
	PPUMASK = 0x2001, // RW
	PPUSTATUS = 0x2002, // R
	OAMADDR = 0x2003, // W
	OAMDATA = 0x2004, // RW
	PPUSCROLL = 0x2005, // W
	PPUADDR = 0x2006, // W
	PPUDATA = 0x2007, // RW
};

// PPU internal registers: https://wiki.nesdev.com/w/index.php?title=PPU_scrolling&redirect=no
class InternalRegister
{
	// yyy NN YYYYY XXXXX
	// ||| || ||||| +++++-- coarse X scroll
	// ||| || +++++-------- coarse Y scroll
	// ||| ++-------------- nametable select
	// +++----------------- fine Y scroll
	// Current VRAM address (15 bits), Note that while the v _register has 15 bits, the PPU memory space is only 14 bits wide.
	// The highest bit is unused for access through $2007.
	public int v;
	public int t; // Temporary VRAM address (15 bits)
	public int x; // Fine X scroll (3 bits)
	public int w; // First or second write toggle (1 bit)
};

// Refer to: https://wiki.nesdev.com/w/index.php/PPU_rendering
class ILatchs
{
	public int nameTable;
	public int attributeTable; // 2bit
	public int lowBackgorundTailByte;
	public int highBackgorundTailByte;
};

class IShiftRegister
{
	public int lowBackgorundTailBytes; // Includes tow tail byte
	public int highBackgorundTailBytes; // Includes tow tail byte
	public int lowBackgroundAttributeByes;
	public int highBackgroundAttributeByes;
};

class ISprite
{
	public int y;
	public int tileIndex;
	public int attributes;
	public int x;
	public bool isZero;
};

enum SpriteAttribute
{
	PALETTE_L = 0x01,
	PALETTE_H = 0x02,
	PRIORITY = 0x20,
	FLIP_H = 0x40,
	FLIP_V = 0x80,
};

enum SpritePixel
{
	PALETTE = 0x3F,
	BEHIND_BG = 0x40,
	ZERO = 0x80,
};

class Status : IStatus
{
	public override int data
	{
		get
		{
			return (this.isSpriteOverflow ? 0x20 : 0) |
				(this.isZeroSpriteHit ? 0x40 : 0) |
				(this.isVBlankStarted ? 0x80 : 0);
		}
	}
};


class PPU : IPPU
{
	public IBus bus;
	public IMapper mapper;
	public IInterrupt interrupt;
	public List<int> pixels; // NES color
	public List<int> oamMemory;
	public PPUController controller = new PPUController();
	public Mask mask = new Mask();
	public InternalRegister _register = new InternalRegister();
	public IShiftRegister shiftRegister = new IShiftRegister();
	public ILatchs latchs = new ILatchs();
	public Status status = new Status();
	public int nmiDelay = 0;

	// The PPUDATA read buffer (post-fetch): https://wiki.nesdev.com/w/index.php/PPU_registers#The_PPUDATA_read_buffer_.28post-fetch.29
	public int readBuffer = 0;
	public int frame = 0; // Frame counter
	public int scanLine = 240; // 0 ~ 261
	public int cycle = 340; // 0 ~ 340
	public int oamAddress = 0;
	public List<ISprite> secondaryOam = new List<ISprite>(8);
	public List<int> spritePixels = new List<int>(256);

	// Least significant bits previously written into a PPU _register
	public int previousData = 0;
	public OnFrame onFrame;

	public PPU(OnFrame onFrame1)
	//onFrame(onFrame1),
	//pixels(256 * 240),
	//oamMemory(256)
	{
		onFrame = onFrame1;
		pixels = Enumerable.Repeat(0, 256 * 240).ToList();
		oamMemory = Enumerable.Repeat(0, 256).ToList();
		for (int i = 0; i < secondaryOam.Capacity; ++i)
			secondaryOam.Add(new ISprite());
		spritePixels = Enumerable.Repeat(0, 256).ToList();
	}

	// PPU timing: https://wiki.nesdev.com/w/images/4/4f/Ppu.svg
	public override void clock()
	{
		// For odd frames, the cycle at the end of the scanline is skipped (this is done internally by jumping directly from (339,261) to (0,0)
		// However, this behavior can be bypassed by keeping rendering disabled until after this scanline has passed
		if (this.scanLine == 261 && this.cycle == 339 && (this.frame & 0x01) != 0 && (this.mask.isShowBackground || this.mask.isShowSprite))
		{
			this.updateCycle();
		}

		this.updateCycle();

		if (!this.mask.isShowBackground && !this.mask.isShowSprite)
		{
			return;
		}

		// Scanline 0 - 239: visible lines
		if (0 <= this.scanLine && this.scanLine <= 239)
		{
			// Cycle 0: do nothing

			// Cycle 1 - 64: Clear secondary OAM
			if (1 == this.cycle)
			{
				this.clearSecondaryOam();
			}

			// Cycle 65 - 256: Sprite evaluation for next scanline
			if (65 == this.cycle)
			{
				this.evalSprite();
			}

			// Cycle 1 - 256: fetch NT, AT, tile
			if (1 <= this.cycle && this.cycle <= 256)
			{
				this.shiftBackground();
				this.renderPixel();
				this.fetchTileRelatedData();
			}

			// Cycle 256
			if (this.cycle == 256)
			{
				this.incrementVerticalPosition();
			}

			// Cycle 257
			if (this.cycle == 257)
			{
				this.copyHorizontalBits();
			}

			// Cycle 257 - 320: Sprite fetches
			if (this.cycle == 257)
			{
				this.fetchSprite();
			}

			// Cycle 321 - 336: fetch NT, AT, tile
			if (321 <= this.cycle && this.cycle <= 336)
			{
				this.shiftBackground();
				this.fetchTileRelatedData();
			}

			// Cycle 337 - 340: unused NT fetches
		}

		// Scanline 240 - 260: Do nothing

		// Scanline 261: pre render line
		if (this.scanLine == 261)
		{
			// Cycle 0: do nothing

			// Cycle 1 - 256: fetch NT, AT, tile
			if (1 <= this.cycle && this.cycle <= 256)
			{
				this.shiftBackground();
				this.fetchTileRelatedData();
			}

			// Cycle 256
			if (this.cycle == 256)
			{
				this.incrementVerticalPosition();
			}

			// Cycle 257
			if (this.cycle == 257)
			{
				this.copyHorizontalBits();
			}

			// Cycle 257 - 320: do nothing

			// Cycle 280
			if (this.cycle == 280)
			{
				this.copyVerticalBits();
			}

			// Cycle 321 - 336: fetch NT, AT, tile
			if (321 <= this.cycle && this.cycle <= 336)
			{
				this.shiftBackground();
				this.fetchTileRelatedData();
			}
		}
	}

	public override int cpuRead(int address)
	{
		switch ((Register)address)
		{
			case Register.PPUCTRL:
				return this.readCtrl();
			case Register.PPUMASK:
				return this.readMask();
			case Register.PPUSTATUS:
				return this.readStatus();
			case Register.OAMADDR:
				return 0;
			case Register.OAMDATA:
				return this.readOAMData();
			case Register.PPUSCROLL:
				return 0;
			case Register.PPUADDR:
				return 0;
			case Register.PPUDATA:
				return this.readPPUData();
		}
		return 0;
	}

	public override void cpuWrite(int address, int data)
	{
		data &= 0xFF;
		this.previousData = data & 0x1F;

		switch ((Register)address)
		{
			case Register.PPUCTRL:
				this.writeCtrl(data);
				break;
			case Register.PPUMASK:
				this.writeMask(data);
				break;
			case Register.PPUSTATUS:
				break;
			case Register.OAMADDR:
				this.writeOAMAddr(data);
				break;
			case Register.OAMDATA:
				this.writeOAMData(data);
				break;
			case Register.PPUSCROLL:
				this.writeScroll(data);
				break;
			case Register.PPUADDR:
				this.writePPUAddr(data);
				break;
			case Register.PPUDATA:
				this.writePPUData(data);
				break;
		}
	}

	public override void dmaCopy(List<int> data)
	{
		for (int i = 0; i < 256; i++)
		{
			this.oamMemory[(i + this.oamAddress) & 0xFF] = data[i];
		}
	}

	private void writeCtrl(int data)
	{
		this.controller.data = data;

		// t: ....BA.. ........ = d: ......BA
		this._register.t = this._register.t & 0xF3FF | (data & 0x03) << 10;
	}

	int readCtrl()
	{
		return this.controller.data;
	}

	void writeMask(int data)
	{
		this.mask.data = data;
	}

	int readMask()
	{
		return this.mask.data;
	}

	int readStatus()
	{
		int data = this.status.data | this.previousData;

		// Clear VBlank flag
		this.status.isVBlankStarted = false;

		// w:                  = 0
		this._register.w = 0;

		return data;
	}

	void writeOAMAddr(int data)
	{
		this.oamAddress = data;
	}

	int readOAMData()
	{
		return this.oamMemory[this.oamAddress];
	}

	void writeOAMData(int data)
	{
		this.oamMemory[this.oamAddress++ & 0xFF] = data;
	}

	void writeScroll(int data)
	{
		if (this._register.w == 0)
		{
			// t: ....... ...HGFED = d: HGFED...
			// x:              CBA = d: .....CBA
			// w:                  = 1
			this._register.t = this._register.t & 0xFFE0 | data >> 3;
			this._register.x = data & 0x07;
			this._register.w = 1;
		}
		else
		{
			// t: CBA..HG FED..... = d: HGFEDCBA
			// w:                  = 0
			this._register.t = this._register.t & 0x0C1F | (data & 0x07) << 12 | (data & 0xF8) << 2;
			this._register.w = 0;
		}
	}

	void writePPUAddr(int data)
	{
		if (this._register.w == 0)
		{
			// t: .FEDCBA ........ = d: ..FEDCBA
			// t: X...... ........ = 0
			// w:                  = 1
			this._register.t = this._register.t & 0x80FF | (data & 0x3F) << 8;
			this._register.w = 1;
		}
		else
		{
			// t: ....... HGFEDCBA = d: HGFEDCBA
			// v                   = t
			// w:                  = 0
			this._register.t = this._register.t & 0xFF00 | data;
			this._register.v = this._register.t;
			this._register.w = 0;
		}
	}

	int readPPUData()
	{
		int data = this.bus.readByte(this._register.v);

		if (this._register.v <= 0x3EFF)
		{ // Buffered read
			int tmp = this.readBuffer;
			this.readBuffer = data;
			data = tmp;
		}
		else
		{
			this.readBuffer = this.bus.readByte(this._register.v - 0x1000);
		}

		this._register.v += this.controller.vramIncrementStepSize;
		this._register.v &= 0x7FFF;

		return data;
	}

	void writePPUData(int data)
	{
		this.bus.writeByte(this._register.v, data);

		this._register.v += this.controller.vramIncrementStepSize;
	}

	void updateCycle()
	{
		if (this.status.isVBlankStarted && this.controller.isNMIEnabled && this.nmiDelay-- == 0)
		{
			this.interrupt.nmi();
		}

		this.cycle++;
		if (this.cycle > 340)
		{
			this.cycle = 0;
			this.scanLine++;
			if (this.scanLine > 261)
			{
				this.scanLine = 0;
				this.frame++;

				this.onFrame(this.pixels);
			}
		}

		// Set VBlank flag
		if (this.scanLine == 241 && this.cycle == 1)
		{
			this.status.isVBlankStarted = true;

			// Trigger NMI
			if (this.controller.isNMIEnabled)
			{
				this.nmiDelay = 15;
			}
		}

		// Clear VBlank flag and Sprite0 Overflow
		if (this.scanLine == 261 && this.cycle == 1)
		{
			this.status.isVBlankStarted = false;
			this.status.isZeroSpriteHit = false;
			this.status.isSpriteOverflow = false;
		}

		if (this.mask.isShowBackground || this.mask.isShowSprite)
		{
			this.mapper.ppuClockHandle(this.scanLine, this.cycle);
		}
	}

	void fetchTileRelatedData()
	{
		if (!this.mask.isShowBackground)
		{
			return;
		}

		switch (this.cycle & 0x07)
		{
			case 1:
				this.loadBackground();
				this.fetchNameTable();
				break;
			case 3:
				this.fetchAttributeTable();
				break;
			case 5:
				this.fetchLowBackgroundTileByte();
				break;
			case 7:
				this.fetchHighBackgroundTileByte();
				break;
			case 0:
				this.incrementHorizontalPosition();
				break;
		}
	}

	void fetchNameTable()
	{
		int address = 0x2000 | (this._register.v & 0x0FFF);

		this.latchs.nameTable = this.bus.readByte(address);
	}

	void fetchAttributeTable()
	{
		int address = 0x23C0 |
			(this._register.v & 0x0C00) |
			((this._register.v >> 4) & 0x38) |
			((this._register.v >> 2) & 0x07);

		bool isRight = (this._register.v & 0x02) != 0;
		bool isBottom = (this._register.v & 0x40) != 0;

		int offset = (isBottom ? 0x02 : 0) | (isRight ? 0x01 : 0);

		this.latchs.attributeTable = this.bus.readByte(address) >> (offset << 1) & 0x03;
	}

	void fetchLowBackgroundTileByte()
	{
		int address = this.controller.backgroundPatternTableAddress +
			this.latchs.nameTable * 16 +
			(this._register.v >> 12 & 0x07);

		this.latchs.lowBackgorundTailByte = this.bus.readByte(address);
	}

	void fetchHighBackgroundTileByte()
	{
		int address = this.controller.backgroundPatternTableAddress +
			this.latchs.nameTable * 16 +
			(this._register.v >> 12 & 0x07) + 8;

		this.latchs.highBackgorundTailByte = this.bus.readByte(address);
	}

	void loadBackground()
	{
		this.shiftRegister.lowBackgorundTailBytes |= this.latchs.lowBackgorundTailByte;
		this.shiftRegister.highBackgorundTailBytes |= this.latchs.highBackgorundTailByte;
		this.shiftRegister.lowBackgroundAttributeByes |= (this.latchs.attributeTable & 0x01) != 0 ? 0xFF : 0;
		this.shiftRegister.highBackgroundAttributeByes |= (this.latchs.attributeTable & 0x02) != 0 ? 0xFF : 0;
	}

	void shiftBackground()
	{
		if (!this.mask.isShowBackground)
		{
			return;
		}

		this.shiftRegister.lowBackgorundTailBytes <<= 1;
		this.shiftRegister.highBackgorundTailBytes <<= 1;
		this.shiftRegister.lowBackgroundAttributeByes <<= 1;
		this.shiftRegister.highBackgroundAttributeByes <<= 1;
	}

	// Between cycle 328 of a scanline, and 256 of the next scanline
	void incrementHorizontalPosition()
	{
		if ((this._register.v & 0x001F) == 31)
		{
			this._register.v &= ~0x001F;
			this._register.v ^= 0x0400;
		}
		else
		{
			this._register.v += 1;
		}
	}

	// At cycle 256 of each scanline
	void incrementVerticalPosition()
	{
		if ((this._register.v & 0x7000) != 0x7000)
		{
			this._register.v += 0x1000;
		}
		else
		{
			this._register.v &= ~0x7000;
			int y = (this._register.v & 0x03E0) >> 5;
			if (y == 29)
			{
				y = 0;
				this._register.v ^= 0x0800;
			}
			else if (y == 31)
			{
				y = 0;
			}
			else
			{
				y += 1;
			}
			this._register.v = (this._register.v & ~0x03E0) | (y << 5);
		}
	}

	// At cycle 257 of each scanline
	void copyHorizontalBits()
	{
		// v: ....F.. ...EDCBA = t: ....F.. ...EDCBA
		this._register.v = (this._register.v & 0xFBE0) | (this._register.t & ~0xFBE0) & 0x7FFF;
	}

	// During cycles 280 to 304 of the pre-render scanline (end of vblank)
	void copyVerticalBits()
	{
		// v: IHGF.ED CBA..... = t: IHGF.ED CBA.....
		this._register.v = (this._register.v & 0x841F) | (this._register.t & ~0x841F) & 0x7FFF;
	}

	void renderPixel()
	{
		int x = this.cycle - 1;
		int y = this.scanLine;

		int offset = 0x8000 >> this._register.x;
		int bit0 = (this.shiftRegister.lowBackgorundTailBytes & offset) != 0 ? 1 : 0;
		int bit1 = (this.shiftRegister.highBackgorundTailBytes & offset) != 0 ? 1 : 0;
		int bit2 = (this.shiftRegister.lowBackgroundAttributeByes & offset) != 0 ? 1 : 0;
		int bit3 = (this.shiftRegister.highBackgroundAttributeByes & offset) != 0 ? 1 : 0;

		int paletteIndex = bit3 << 3 | bit2 << 2 | bit1 << 1 | bit0 << 0;
		int spritePaletteIndex = this.spritePixels[x] & (int)SpritePixel.PALETTE;

		bool isTransparentSprite = spritePaletteIndex % 4 == 0 || !this.mask.isShowSprite;
		bool isTransparentBackground = paletteIndex % 4 == 0 || !this.mask.isShowBackground;

		int address = 0x3F00;
		if (isTransparentBackground)
		{
			if (isTransparentSprite)
			{
				// Do nothing
			}
			else
			{
				address = 0x3F10 + spritePaletteIndex;
			}
		}
		else
		{
			if (isTransparentSprite)
			{
				address = 0x3F00 + paletteIndex;
			}
			else
			{
				// Sprite 0 hit does not happen:
				//   - If background or sprite rendering is disabled in PPUMASK ($2001)
				//   - At x=0 to x=7 if the left-side clipping window is enabled (if bit 2 or bit 1 of PPUMASK is 0).
				//   - At x=255, for an obscure reason related to the pixel pipeline.
				//   - At any pixel where the background or sprite pixel is transparent (2-bit color index from the CHR pattern is %00).
				//   - If sprite 0 hit has already occurred this frame. Bit 6 of PPUSTATUS ($2002) is cleared to 0 at dot 1 of the pre-render line.
				//     This means only the first sprite 0 hit in a frame can be detected.
				if ((this.spritePixels[x] & (int)SpritePixel.ZERO) != 0)
				{
					if (
						(!this.mask.isShowBackground || !this.mask.isShowSprite) ||
						(0 <= x && x <= 7 && (!this.mask.isShowSpriteLeft8px || !this.mask.isShowBackgroundLeft8px)) ||
						x == 255
						// TODO: Only the first sprite 0 hit in a frame can be detected.
						)
					{
						// Sprite 0 hit does not happen
					}
					else
					{
						this.status.isZeroSpriteHit = true;
					}
				}
				address = (this.spritePixels[x] & (int)SpritePixel.BEHIND_BG) != 0 ? 0x3F00 + paletteIndex : 0x3F10 + spritePaletteIndex;
			}
		}

		this.pixels[x + y * 256] = this.bus.readByte(address);
	}

	void clearSecondaryOam()
	{
		if (!this.mask.isShowSprite)
		{
			return;
		}

		foreach (var oam in secondaryOam)
		{
			oam.attributes = 0xFF;
			oam.tileIndex = 0xFF;
			oam.x = 0xFF;
			oam.y = 0xFF;
		};
	}

	void evalSprite()
	{
		if (!this.mask.isShowSprite)
		{
			return;
		}

		int spriteCount = 0;

		// Find eligible sprites
		for (int i = 0; i < 64; i++)
		{
			int y = this.oamMemory[i * 4];
			if (this.scanLine < y || (this.scanLine >= y + (int)this.controller.spriteSize))
			{
				continue;
			}

			// Overflow?
			if (spriteCount == 8)
			{
				this.status.isSpriteOverflow = true;
				break;
			}

			var oam = this.secondaryOam[spriteCount++];
			oam.y = y;
			oam.tileIndex = this.oamMemory[i * 4 + 1];
			oam.attributes = this.oamMemory[i * 4 + 2];
			oam.x = this.oamMemory[i * 4 + 3];
			oam.isZero = i == 0;
		}
	}

	void fetchSprite()
	{
		if (!this.mask.isShowSprite)
		{
			return;
		}

		for (int i = 0; i < spritePixels.Count; ++i)
			spritePixels[i] = 0;

		for (var i = secondaryOam.Count - 1; i >= 0; --i)
		{
			var sprite = secondaryOam[i];
			// Hidden sprite?
			if (sprite.y >= 0xEF)
			{
				continue;
			}

			bool isBehind = (sprite.attributes & (int)SpriteAttribute.PRIORITY) != 0;
			bool isZero = sprite.isZero;
			bool isFlipH = (sprite.attributes & (int)SpriteAttribute.FLIP_H) != 0;
			bool isFlipV = (sprite.attributes & (int)SpriteAttribute.FLIP_V) != 0;

			// Caculate tile address
			int address;
			if (this.controller.spriteSize == SpriteSize.SIZE_8X8)
			{
				int baseAddress = this.controller.spritePatternTableAddress + (sprite.tileIndex << 4);
				int offset = isFlipV ? (7 - this.scanLine + sprite.y) : (this.scanLine - sprite.y);
				address = baseAddress + offset;
			}
			else
			{
				int baseAddress = ((sprite.tileIndex & 0x01) != 0 ? 0x1000 : 0x0000) + ((sprite.tileIndex & 0xFE) << 4);
				int offset = isFlipV ? (15 - this.scanLine + sprite.y) : (this.scanLine - sprite.y);
				address = baseAddress + offset % 8 + (int)Math.Floor((double)offset / 8) * 16;
			}

			// Fetch tile data
			int tileL = this.bus.readByte(address);
			int tileH = this.bus.readByte(address + 8);

			// Generate sprite pixels
			for (int j = 0; j < 8; j++)
			{
				int b = isFlipH ? 0x01 << j : 0x80 >> j;

				int bit0 = (tileL & b) != 0 ? 1 : 0;
				int bit1 = (tileH & b) != 0 ? 1 : 0;
				int bit2 = (sprite.attributes & (int)SpriteAttribute.PALETTE_L) != 0 ? 1 : 0;
				int bit3 = (sprite.attributes & (int)SpriteAttribute.PALETTE_H) != 0 ? 1 : 0;
				int index = bit3 << 3 | bit2 << 2 | bit1 << 1 | bit0;

				if (index % 4 == 0 && (this.spritePixels[sprite.x + j] & (int)SpritePixel.PALETTE) % 4 != 0)
				{
					continue;
				}

				this.spritePixels[sprite.x + j] = index |
					(isBehind ? (int)SpritePixel.BEHIND_BG : 0) |
					(isZero ? (int)SpritePixel.ZERO : 0);
			}
		}
	}
};


class RAM : IRAM
{
	List<int> ram;
	int size;
	int offset;
	public RAM(int size1, int offset1 = 0)
	{
		size = size1;
		offset = offset1;
		ram = Enumerable.Repeat(0, size).ToList();
	}

	public override int read(int address)
	{
		address = (address - this.offset) & 0xFFFF;

		return this.ram[address];
	}

	public override void write(int address, int data)
	{
		address = (address - this.offset) & 0xFFFF;

		this.ram[address] = data;
	}
};

// PPU memory map: https://wiki.nesdev.com/w/index.php/PPU_memory_map
class PPUBus : IBus
{
	public ICartridge cartridge;
	public IRAM ram; // 2K
	public IRAM backgroundPallette; // 16B
	public IRAM spritePallette; // 16B

	public int readByte(int address)
	{
		address &= 0x3FFF;

		if (address < 0x2000)
		{
			// Pattern table 0 - 1
			return this.cartridge.mapper.read(address);
		}
		else if (address < 0x3000)
		{
			// Nametable 0 - 3
			return this.ram.read(this.parseMirrorAddress(address));
		}
		else if (address < 0x3F00)
		{
			// Mirrors of $2000-$2EFF
			return this.readByte(address - 0x1000);
		}
		else
		{
			// Palette RAM indexes
			address &= 0x3F1F;

			if (address < 0x3F10)
			{ // Background pallette
				return this.backgroundPallette.read(address);
			}
			else
			{ // Sprite pallette
			  // Refer to https://wiki.nesdev.com/w/index.php/PPU_palettes
			  // Addresses $3F10/$3F14/$3F18/$3F1C are mirrors of $3F00/$3F04/$3F08/$3F0C
				if ((address & 3) == 0)
				{
					address -= 0x10;
					return this.backgroundPallette.read(address);
				}

				return this.spritePallette.read(address);
			}
		}
	}

	public void writeByte(int address, int data)
	{
		address &= 0x3FFF;

		if (address < 0x2000)
		{
			// Pattern table 0 - 1
			this.cartridge.mapper.write(address, data);
		}
		else if (address < 0x3000)
		{
			// Nametable 0 - 3
			this.ram.write(this.parseMirrorAddress(address), data);
		}
		else if (address < 0x3F00)
		{
			// Mirrors of $2000-$2EFF
			this.writeByte(address - 0x1000, data);
			return;
		}
		else
		{
			// Palette RAM indexes
			address &= 0x3F1F;

			if (address < 0x3F10)
			{ // Background pallette
				this.backgroundPallette.write(address, data);
			}
			else
			{ // Sprite pallette
			  // Refer to https://wiki.nesdev.com/w/index.php/PPU_palettes
			  // Addresses $3F10/$3F14/$3F18/$3F1C are mirrors of $3F00/$3F04/$3F08/$3F0C
				if ((address & 3) == 0)
				{
					address -= 0x10;
					this.backgroundPallette.write(address, data);
					return;
				}

				this.spritePallette.write(address, data);
			}
		}
	}

	public int readWord(int address)
	{
		return this.readByte(address + 1) << 8 | this.readByte(address);
	}

	public void writeWord(int address, int data)
	{
		this.writeByte(address, data);
		this.writeByte(address + 1, data >> 8);
	}

	int parseMirrorAddress(int address)
	{
		switch (this.cartridge.info.mirror)
		{
			case Mirror.HORIZONTAL:
				return (address & 0x23FF) | ((address & 0x0800) != 0 ? 0x0400 : 0);
			case Mirror.VERTICAL:
				return address & 0x27FF;
			case Mirror.FOUR_SCREEN:
				return address;
			case Mirror.SINGLE_SCREEN_LOWER_BANK:
				return address & 0x23FF;
			case Mirror.SINGLE_SCREEN_UPPER_BANK:
				return address & 0x23FF + 0x0400;
			default:
				throw new Exception("Invalid mirror type : '${this.cartridge.info.mirror}");
		}
	}
};

enum Header
{
	PRG = 4,
	CHR = 5,
	FLAG1 = 6,
	FLAG2 = 7,
};

// INES: https://wiki.nesdev.com/w/index.php/INES
class Cartridge : ICartridge
{
	public Cartridge(List<int> data, List<int> sram)
	{
		this.info = new IROMInfo();

		Cartridge.checkConstant(data);

		this.parseROMInfo(data);

		var prgOffset = this.info.isTrained ? 16 + 512 : 16;
		var prg = data.GetRange(prgOffset, this.info.prg * 16 * 1024);
		var a = data[16 + 171661];

		var chrOffset = prgOffset + prg.Count;
		var chr = data.GetRange(chrOffset, this.info.chr * 8 * 1024);

		switch (this.info.mapper)
		{
			case 0:
				this.mapper = new Mapper0(this, sram, prg, chr);
				break;
			case 1:
				this.mapper = new Mapper1(this, sram, prg, chr);
				break;
			case 2:
				this.mapper = new Mapper2(this, sram, prg, chr);
				break;
			case 3:
				this.mapper = new Mapper3(this, sram, prg, chr);
				break;
			case 4:
				this.mapper = new Mapper4(this, sram, prg, chr);
				break;
			case 74:
				this.mapper = new Mapper74(this, sram, prg, chr);
				break;
			case 242:
				this.mapper = new Mapper242(this, sram, prg, chr);
				break;
			default:
				throw new Exception("Unsupported mapper: ${this.info.mapper}");
		}
	}

	private void parseROMInfo(List<int> data)
	{
		this.info.prg = data[(int)Header.PRG];
		this.info.chr = data[(int)Header.CHR];

		var mapperL = data[(int)Header.FLAG1] >> 4;
		var mapperH = data[(int)Header.FLAG2] >> 4;
		this.info.mapper = mapperH << 4 | mapperL;

		this.info.mirror = (data[(int)Header.FLAG1] & 0x08) != 0 ? Mirror.FOUR_SCREEN :
			(data[(int)Header.FLAG1] & 0x01) != 0 ? Mirror.VERTICAL : Mirror.HORIZONTAL;

		this.info.hasBatteryBacked = (data[(int)Header.FLAG1] & 0x02) != 0;
		this.info.isTrained = (data[(int)Header.FLAG1] & 0x04) != 0;
	}

	private static void checkConstant(List<int> data)
	{
		var str = "NES\x1a";
		for (var i = 0; i < str.Length; i++)
		{
			if (data[i] != str[i])
			{
				throw new Exception("Invalid nes file");
			}
		}

		if ((data[7] & 0x0C) == 0x08)
		{
			throw new Exception("NES2.0 is not currently supported");
		}
	}
}


// Standard controller: http://wiki.nesdev.com/w/index.php/Standard_controller
class StandardController : IStandardController
{
	int data = 0;
	bool isStrobe = false;
	int offset = 0;

	public void updateButton(StandardControllerButton button, bool isPressDown)
	{
		if (isPressDown)
		{
			this.data |= (int)button;
		}
		else
		{
			this.data &= ~(int)button & 0xFF;
		}
	}

	public void write(int data)
	{
		if ((data & 0x01) != 0)
		{
			this.isStrobe = true;
		}
		else
		{
			this.offset = 0;
			this.isStrobe = false;
		}
	}

	public int read()
	{
		int data = this.isStrobe ? this.data & (int)StandardControllerButton.A : this.data & (0x80 >> this.offset++);

		return data != 0 ? 1 : 0;
	}
};


enum InterruptVector
{
	NMI = 0xFFFA,
	RESET = 0xFFFC,
	IRQ = 0xFFFE,
};

class AddressData
{
	public int address; // Set value to Global.NaN if immediate mode
	public int data; // Set value to Global.NaN if not immediate mode
	public bool isCrossPage;

	public AddressData(int a, int d, bool c)
	{
		address = a;
		data = d;
		isCrossPage = c;
	}
};

enum Instruction
{
	ADC, AND, ASL, BCC, BCS, BEQ, BIT, BMI,
	BNE, BPL, BRK, BVC, BVS, CLC, CLD, CLI,
	CLV, CMP, CPX, CPY, DEC, DEX, DEY, EOR,
	INC, INX, INY, JMP, JSR, LDA, LDX, LDY,
	LSR, NOP, ORA, PHA, PHP, PLA, PLP, ROL,
	ROR, RTI, RTS, SBC, SEC, SED, SEI, STA,
	STX, STY, TAX, TAY, TSX, TXA, TXS, TYA,

	// Illegal opcode
	DCP, ISC, LAX, RLA, RRA, SAX, SLO, SRE,

	INVALID,
	UNDEFINED,
};

// Refer to http://obelisk.me.uk/6502/addressing.html#IMP
enum AddressingMode
{
	IMPLICIT, // CLC | RTS
	ACCUMULATOR, // LSR A
	IMMEDIATE, // LDA #10
	ZERO_PAGE, // LDA $00
	ZERO_PAGE_X, // STY $10, X
	ZERO_PAGE_Y, // LDX $10, Y
	RELATIVE, // BEQ label | BNE *+4
	ABSOLUTE, // JMP $1234
	ABSOLUTE_X, // STA $3000, X
	ABSOLUTE_Y, // AND $4000, Y
	INDIRECT, // JMP ($FFFC)
	X_INDEXED_INDIRECT, // LDA ($40, X)
	INDIRECT_Y_INDEXED, // LDA ($40), Y
};




delegate void Func1(AddressData addrData);
delegate AddressData Func2();



// 6502 Instruction Reference: http://obelisk.me.uk/6502/reference.html
// 6502/6510/8500/8502 Opcode matrix: http://www.oxyron.de/html/opcodes02.html
class CPU : ICPU
{
	public IBus bus;
	public int suspendCycles = 0;
	public IBus GetBus() { return bus; }
	public int clocks = 0;
	public int deferCycles = 0;
	public IRegisters registers = new IRegisters();



	Dictionary<Instruction, Func1> instructionMap;
	Dictionary<AddressingMode, Func2> addressingModeMap;

	public CPU()
	{
		instructionMap = new Dictionary<Instruction, Func1>{
	{ Instruction.ADC, this.adc },
	{ Instruction.AND, this.and },
	{ Instruction.ASL, this.asl },
	{ Instruction.BCC, this.bcc },
	{ Instruction.BCS, this.bcs },
	{ Instruction.BEQ, this.beq },
	{ Instruction.BIT, this.bit },
	{ Instruction.BMI, this.bmi },
	{ Instruction.BNE, this.bne },
	{ Instruction.BPL, this.bpl },
	{ Instruction.BRK, this.brk },
	{ Instruction.BVC, this.bvc },
	{ Instruction.BVS, this.bvs },
	{ Instruction.CLC, this.clc },
	{ Instruction.CLD, this.cld },
	{ Instruction.CLI, this.cli },
	{ Instruction.CLV, this.clv },
	{ Instruction.CMP, this.cmp },
	{ Instruction.CPX, this.cpx },
	{ Instruction.CPY, this.cpy },
	{ Instruction.DEC, this.dec },
	{ Instruction.DEX, this.dex },
	{ Instruction.DEY, this.dey },
	{ Instruction.EOR, this.eor },
	{ Instruction.INC, this.inc },
	{ Instruction.INX, this.inx },
	{ Instruction.INY, this.iny },
	{ Instruction.JMP, this.jmp },
	{ Instruction.JSR, this.jsr },
	{ Instruction.LDA, this.lda },
	{ Instruction.LDX, this.ldx },
	{ Instruction.LDY, this.ldy },
	{ Instruction.LSR, this.lsr },
	{ Instruction.NOP, this.nop },
	{ Instruction.ORA, this.ora },
	{ Instruction.PHA, this.pha },
	{ Instruction.PHP, this.php },
	{ Instruction.PLA, this.pla },
	{ Instruction.PLP, this.plp },
	{ Instruction.ROL, this.rol },
	{ Instruction.ROR, this.ror },
	{ Instruction.RTI, this.rti },
	{ Instruction.RTS, this.rts },
	{ Instruction.SBC, this.sbc },
	{ Instruction.SEC, this.sec },
	{ Instruction.SED, this.sed },
	{ Instruction.SEI, this.sei },
	{ Instruction.STA, this.sta },
	{ Instruction.STX, this.stx },
	{ Instruction.STY, this.sty },
	{ Instruction.TAX, this.tax },
	{ Instruction.TAY, this.tay },
	{ Instruction.TSX, this.tsx },
	{ Instruction.TXA, this.txa },
	{ Instruction.TXS, this.txs },
	{ Instruction.TYA, this.tya },

	// Illegal instruction
	{ Instruction.DCP, this.dcp },
	{ Instruction.ISC, this.isc },
	{ Instruction.LAX, this.lax },
	{ Instruction.RLA, this.rla },
	{ Instruction.RRA, this.rra },
	{ Instruction.SAX, this.sax },
	{ Instruction.SLO, this.slo },
	{ Instruction.SRE, this.sre },
  };

		addressingModeMap = new Dictionary<AddressingMode, Func2>{
	{ AddressingMode.ABSOLUTE, this.absolute },
	{ AddressingMode.ABSOLUTE_X, this.absoluteX },
	{ AddressingMode.ABSOLUTE_Y, this.absoluteY },
	{ AddressingMode.ACCUMULATOR, this.accumulator },
	{ AddressingMode.IMMEDIATE, this.immediate },
	{ AddressingMode.IMPLICIT, this.implicit1 },
	{ AddressingMode.INDIRECT, this.indirect },
	{ AddressingMode.INDIRECT_Y_INDEXED, this.indirectYIndexed },
	{ AddressingMode.RELATIVE, this.relative },
	{ AddressingMode.X_INDEXED_INDIRECT, this.xIndexedIndirect },
	{ AddressingMode.ZERO_PAGE, this.zeroPage },
	{ AddressingMode.ZERO_PAGE_X, this.zeroPageX },
	{ AddressingMode.ZERO_PAGE_Y, this.zeroPageY },
  };
	}

	public void reset()
	{
		this.registers.A = 0;
		this.registers.X = 0;
		this.registers.Y = 0;
		this.registers.P = 0;
		this.registers.SP = 0xfd;
		this.registers.PC = this.bus.readWord((int)InterruptVector.RESET);

		this.deferCycles = 8;
		this.clocks = 0;
	}

	public void clock()
	{
		if (this.suspendCycles > 0)
		{
			this.suspendCycles--;
			return;
		}

		if (this.deferCycles == 0)
		{
			this.step();
		}

		this.deferCycles--;
		this.clocks++;
	}

	public void irq()
	{
		if (this.isFlagSet(Flags.I))
		{
			return;
		}

		this.pushWord(this.registers.PC);
		this.pushByte((this.registers.P | (int)Flags.U) & ~(int)Flags.B);

		this.setFlag(Flags.I, true);

		this.registers.PC = this.bus.readWord((int)InterruptVector.IRQ);

		this.deferCycles += 7;
	}


	public void nmi()
	{
		this.pushWord(this.registers.PC);
		this.pushByte((this.registers.P | (int)Flags.U) & ~(int)Flags.B);

		this.setFlag(Flags.I, true);

		this.registers.PC = this.bus.readWord((int)InterruptVector.NMI);

		this.deferCycles += 7;
	}

	void setFlag(Flags flag, bool value)
	{
		if (value)
		{
			this.registers.P |= (int)flag;
		}
		else
		{
			this.registers.P &= ~(int)flag;
		}
	}

	bool isFlagSet(Flags flag)
	{
		return (this.registers.P & (int)flag) != 0;
	}

	void step()
	{
		int opcode = this.bus.readByte(this.registers.PC++);
		var entry = Global.OPCODE_TABLE[opcode];
		if (entry.instruction == Instruction.UNDEFINED)
		{
			throw new Exception("Invalid opcode '${opcode}(0x${opcode.toString(16)})', pc: 0x${ (this.registers.PC - 1).toString(16) }");
		}

		if (entry.instruction == Instruction.INVALID)
		{
			return;
		}

		if (!this.addressingModeMap.ContainsKey(entry.addressMode))
		{
			throw new Exception("Unsuppored addressing mode : ${ AddressingMode[entry.addressingMode] }");
		}

		var addrModeFunc = this.addressingModeMap[entry.addressMode];
		AddressData ret = addrModeFunc();
		if (ret.isCrossPage)
		{
			this.deferCycles += entry.pageCycles;
		}

		if (!this.instructionMap.ContainsKey(entry.instruction))
		{
			throw new Exception("Unsupported instruction: ${ Instruction[entry.instruction] }");
		}
		var instrFunc = this.instructionMap[entry.instruction];
		instrFunc(ret);

		this.deferCycles += entry.cycles;
	}

	void pushWord(int data)
	{
		this.pushByte(data >> 8);
		this.pushByte(data);
	}

	void pushByte(int data)
	{
		this.bus.writeByte(0x100 + this.registers.SP, data);
		this.registers.SP = (this.registers.SP - 1) & 0xFF;
	}

	int popWord()
	{
		return this.popByte() | this.popByte() << 8;
	}

	int popByte()
	{
		this.registers.SP = (this.registers.SP + 1) & 0xFF;
		return this.bus.readByte(0x100 + this.registers.SP);
	}

	void setNZFlag(int data)
	{
		this.setFlag(Flags.Z, (data & 0xFF) == 0);
		this.setFlag(Flags.N, (data & 0x80) != 0);
	}

	int getData(AddressData addrData)
	{
		if (!Global.isNaN(addrData.data))
		{
			return addrData.data;
		}
		else
		{
			return this.bus.readByte(addrData.address);
		}
	}

	AddressData absolute()
	{
		int address = this.bus.readWord(this.registers.PC);
		this.registers.PC += 2;

		return new AddressData(address & 0xFFFF, Global.NaN, false);
	}

	AddressData absoluteX()
	{
		int baseAddress = this.bus.readWord(this.registers.PC);
		this.registers.PC += 2;

		int address = baseAddress + this.registers.X;

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			this.isCrossPage(baseAddress, address)
		);
	}

	AddressData absoluteY()
	{
		int baseAddress = this.bus.readWord(this.registers.PC);
		this.registers.PC += 2;
		int address = baseAddress + this.registers.Y;

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			this.isCrossPage(baseAddress, address)
		);
	}

	AddressData accumulator()
	{
		return new AddressData(
			Global.NaN,
			this.registers.A,
			false);
	}

	AddressData immediate()
	{
		return new AddressData(
			Global.NaN,
			this.bus.readByte(this.registers.PC++),
			false);
	}

	AddressData implicit1()
	{
		return new AddressData(
			Global.NaN,
			Global.NaN,
			false);
	}

	AddressData indirect()
	{
		int address = this.bus.readWord(this.registers.PC);
		this.registers.PC += 2;

		if ((address & 0xFF) == 0xFF)
		{ // Hardware bug
			address = this.bus.readByte(address & 0xFF00) << 8 | this.bus.readByte(address);
		}
		else
		{
			address = this.bus.readWord(address);
		}

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			false);
	}

	AddressData indirectYIndexed()
	{
		int value = this.bus.readByte(this.registers.PC++);

		int l = this.bus.readByte(value & 0xFF);
		int h = this.bus.readByte((value + 1) & 0xFF);

		int baseAddress = h << 8 | l;
		int address = baseAddress + this.registers.Y;

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			this.isCrossPage(baseAddress, address));
	}

	AddressData relative()
	{
		// Range is -128 ~ 127
		int offset = this.bus.readByte(this.registers.PC++);
		if ((offset & 0x80) != 0)
		{
			offset = offset - 0x100;
		}

		return new AddressData(
			(this.registers.PC + offset) & 0xFFFF,
			Global.NaN,
			false);
	}

	AddressData xIndexedIndirect()
	{
		int value = this.bus.readByte(this.registers.PC++);
		int address = (value + this.registers.X);

		int l = this.bus.readByte(address & 0xFF);
		int h = this.bus.readByte((address + 1) & 0xFF);

		return new AddressData(
			(h << 8 | l) & 0xFFFF,
			Global.NaN,
			false);
	}

	AddressData zeroPage()
	{
		int address = this.bus.readByte(this.registers.PC++);

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			false);
	}

	AddressData zeroPageX()
	{
		int address = (this.bus.readByte(this.registers.PC++) + this.registers.X) & 0xFF;

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			false);
	}

	AddressData zeroPageY()
	{
		int address = (this.bus.readByte(this.registers.PC++) + this.registers.Y) & 0xFF;

		return new AddressData(
			address & 0xFFFF,
			Global.NaN,
			false);
	}

	void adc(AddressData addrData)
	{
		int data = this.getData(addrData);
		int value = data + this.registers.A + (this.isFlagSet(Flags.C) ? 1 : 0);

		this.setFlag(Flags.C, value > 0xFF);
		this.setFlag(Flags.V, ((~(this.registers.A ^ data) & (this.registers.A ^ value)) & 0x80) != 0);
		this.setNZFlag(value);

		this.registers.A = value & 0xFF;
	}

	void and(AddressData addrData)
	{
		this.registers.A &= this.getData(addrData);
		this.setNZFlag(this.registers.A);
	}

	void asl(AddressData addrData)
	{
		int data = this.getData(addrData) << 1;

		this.setFlag(Flags.C, (data & 0x100) != 0);
		data = data & 0xFF;
		this.setNZFlag(data);

		if (Global.isNaN(addrData.address))
		{
			this.registers.A = data;
		}
		else
		{
			this.bus.writeByte(addrData.address, data);
		}
	}

	void bcc(AddressData addrData)
	{
		if (!this.isFlagSet(Flags.C))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void bcs(AddressData addrData)
	{
		if (this.isFlagSet(Flags.C))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void beq(AddressData addrData)
	{
		if (this.isFlagSet(Flags.Z))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void bit(AddressData addrData)
	{
		int data = this.getData(addrData);

		this.setFlag(Flags.Z, (this.registers.A & data) == 0);
		this.setFlag(Flags.N, (data & (1 << 7)) != 0);
		this.setFlag(Flags.V, (data & (1 << 6)) != 0);
	}

	void bmi(AddressData addrData)
	{
		if (this.isFlagSet(Flags.N))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void bne(AddressData addrData)
	{
		if (!this.isFlagSet(Flags.Z))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void bpl(AddressData addrData)
	{
		if (!this.isFlagSet(Flags.N))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void brk(AddressData addrData)
	{
		this.pushWord(this.registers.PC);
		this.pushByte(this.registers.P | (int)Flags.B | (int)Flags.U);

		this.setFlag(Flags.I, true);

		this.registers.PC = this.bus.readWord((int)InterruptVector.IRQ);
	}

	void bvc(AddressData addrData)
	{
		if (!this.isFlagSet(Flags.V))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void bvs(AddressData addrData)
	{
		if (this.isFlagSet(Flags.V))
		{
			this.deferCycles++;
			if (this.isCrossPage(this.registers.PC, addrData.address))
			{
				this.deferCycles++;
			}

			this.registers.PC = addrData.address;
		}
	}

	void clc(AddressData addrData)
	{
		this.setFlag(Flags.C, false);
	}

	void cld(AddressData addrData)
	{
		this.setFlag(Flags.D, false);
	}

	void cli(AddressData addrData)
	{
		this.setFlag(Flags.I, false);
	}

	void clv(AddressData addrData)
	{
		this.setFlag(Flags.V, false);
	}

	void cmp(AddressData addrData)
	{
		int data = this.getData(addrData);
		int res = this.registers.A - data;

		this.setFlag(Flags.C, this.registers.A >= data);
		this.setNZFlag(res);
	}

	void cpx(AddressData addrData)
	{
		int data = this.getData(addrData);
		int res = this.registers.X - data;

		this.setFlag(Flags.C, this.registers.X >= data);
		this.setNZFlag(res);
	}

	void cpy(AddressData addrData)
	{
		int data = this.getData(addrData);
		int res = this.registers.Y - data;

		this.setFlag(Flags.C, this.registers.Y >= data);
		this.setNZFlag(res);
	}

	void dec(AddressData addrData)
	{
		int data = (this.getData(addrData) - 1) & 0xFF;

		this.bus.writeByte(addrData.address, data);
		this.setNZFlag(data);
	}

	void dex(AddressData addrData)
	{
		this.registers.X = (this.registers.X - 1) & 0xFF;
		this.setNZFlag(this.registers.X);
	}

	void dey(AddressData addrData)
	{
		this.registers.Y = (this.registers.Y - 1) & 0xFF;
		this.setNZFlag(this.registers.Y);
	}

	void eor(AddressData addrData)
	{
		this.registers.A ^= this.getData(addrData);
		this.setNZFlag(this.registers.A);
	}

	void inc(AddressData addrData)
	{
		int data = (this.getData(addrData) + 1) & 0xFF;

		this.bus.writeByte(addrData.address, data);
		this.setNZFlag(data);
	}

	void inx(AddressData addrData)
	{
		this.registers.X = (this.registers.X + 1) & 0xFF;
		this.setNZFlag(this.registers.X);
	}

	void iny(AddressData addrData)
	{
		this.registers.Y = (this.registers.Y + 1) & 0xFF;
		this.setNZFlag(this.registers.Y);
	}

	void jmp(AddressData addrData)
	{
		this.registers.PC = addrData.address;
	}

	void jsr(AddressData addrData)
	{
		this.pushWord(this.registers.PC - 1);
		this.registers.PC = addrData.address;
	}

	void lda(AddressData addrData)
	{
		this.registers.A = this.getData(addrData);

		this.setNZFlag(this.registers.A);
	}

	void ldx(AddressData addrData)
	{
		this.registers.X = this.getData(addrData);

		this.setNZFlag(this.registers.X);
	}

	void ldy(AddressData addrData)
	{
		this.registers.Y = this.getData(addrData);

		this.setNZFlag(this.registers.Y);
	}

	void lsr(AddressData addrData)
	{
		int data = this.getData(addrData);

		this.setFlag(Flags.C, (data & 0x01) != 0);
		data >>= 1;
		this.setNZFlag(data);

		if (Global.isNaN(addrData.address))
		{
			this.registers.A = data;
		}
		else
		{
			this.bus.writeByte(addrData.address, data);
		}
	}

	void nop(AddressData addrData)
	{
		// Do nothing
	}

	void ora(AddressData addrData)
	{
		this.registers.A |= this.getData(addrData);
		this.setNZFlag(this.registers.A);
	}

	void pha(AddressData addrData)
	{
		this.pushByte(this.registers.A);
	}

	void php(AddressData addrData)
	{
		this.pushByte(this.registers.P | (int)Flags.B | (int)Flags.U);
	}

	void pla(AddressData addrData)
	{
		this.registers.A = this.popByte();
		this.setNZFlag(this.registers.A);
	}

	void plp(AddressData addrData)
	{
		this.registers.P = this.popByte();
		this.setFlag(Flags.B, false);
		this.setFlag(Flags.U, true);
	}

	void rol(AddressData addrData)
	{
		int data = this.getData(addrData);

		bool isCarry = this.isFlagSet(Flags.C);
		this.setFlag(Flags.C, (data & 0x80) != 0);
		data = (data << 1 | (isCarry ? 1 : 0)) & 0xFF;
		this.setNZFlag(data);

		if (Global.isNaN(addrData.address))
		{
			this.registers.A = data;
		}
		else
		{
			this.bus.writeByte(addrData.address, data);
		}
	}

	void ror(AddressData addrData)
	{
		int data = this.getData(addrData);

		bool isCarry = this.isFlagSet(Flags.C);
		this.setFlag(Flags.C, (data & 1) != 0);
		data = data >> 1 | (isCarry ? 1 << 7 : 0);
		this.setNZFlag(data);

		if (Global.isNaN(addrData.address))
		{
			this.registers.A = data;
		}
		else
		{
			this.bus.writeByte(addrData.address, data);
		}
	}

	void rti(AddressData addrData)
	{
		this.registers.P = this.popByte();
		this.setFlag(Flags.B, false);
		this.setFlag(Flags.U, true);

		this.registers.PC = this.popWord();
	}

	void rts(AddressData addrData)
	{
		this.registers.PC = this.popWord() + 1;
	}

	void sbc(AddressData addrData)
	{
		int data = this.getData(addrData);
		int res = this.registers.A - data - (this.isFlagSet(Flags.C) ? 0 : 1);

		this.setNZFlag(res);
		this.setFlag(Flags.C, res >= 0);
		this.setFlag(Flags.V, ((res ^ this.registers.A) & (res ^ data ^ 0xFF) & 0x0080) != 0);

		this.registers.A = res & 0xFF;
	}

	void sec(AddressData addrData)
	{
		this.setFlag(Flags.C, true);
	}

	void sed(AddressData addrData)
	{
		this.setFlag(Flags.D, true);
	}

	void sei(AddressData addrData)
	{
		this.setFlag(Flags.I, true);
	}

	void sta(AddressData addrData)
	{
		this.bus.writeByte(addrData.address, this.registers.A);
	}

	void stx(AddressData addrData)
	{
		this.bus.writeByte(addrData.address, this.registers.X);
	}

	void sty(AddressData addrData)
	{
		this.bus.writeByte(addrData.address, this.registers.Y);
	}

	void tax(AddressData addrData)
	{
		this.registers.X = this.registers.A;
		this.setNZFlag(this.registers.X);
	}

	void tay(AddressData addrData)
	{
		this.registers.Y = this.registers.A;
		this.setNZFlag(this.registers.Y);
	}

	void tsx(AddressData addrData)
	{
		this.registers.X = this.registers.SP;
		this.setNZFlag(this.registers.X);
	}

	void txa(AddressData addrData)
	{
		this.registers.A = this.registers.X;
		this.setNZFlag(this.registers.A);
	}

	void txs(AddressData addrData)
	{
		this.registers.SP = this.registers.X;
	}

	void tya(AddressData addrData)
	{
		this.registers.A = this.registers.Y;
		this.setNZFlag(this.registers.A);
	}

	// Illegal instruction
	void dcp(AddressData addrData)
	{
		this.dec(addrData);
		this.cmp(addrData);
	}

	void isc(AddressData addrData)
	{
		this.inc(addrData);
		this.sbc(addrData);
	}

	void lax(AddressData addrData)
	{
		this.lda(addrData);
		this.ldx(addrData);
	}

	void rla(AddressData addrData)
	{
		this.rol(addrData);
		this.and(addrData);
	}

	void rra(AddressData addrData)
	{
		this.ror(addrData);
		this.adc(addrData);
	}

	void sax(AddressData addrData)
	{
		int value = this.registers.A & this.registers.X;
		this.bus.writeByte(addrData.address, value);
	}

	void slo(AddressData addrData)
	{
		this.asl(addrData);
		this.ora(addrData);
	}

	void sre(AddressData addrData)
	{
		this.lsr(addrData);
		this.eor(addrData);
	}

	bool isCrossPage(int addr1, int addr2)
	{
		return (addr1 & 0xff00) != (addr2 & 0xff00);
	}
};

class CPUBus : IBus
{
	public ICartridge cartridge;
	public IRAM ram;
	public IPPU ppu;
	public IDMA dma;
	public IController controller1;
	public IController controller2;
	public IAPU apu;

	public void writeByte(int address, int data)
	{
		if (address < 0x2000)
		{
			// RAM
			this.ram.write(address & 0x07FF, data);
		}
		else if (address < 0x4000)
		{
			// PPU Registers
			this.ppu.cpuWrite(address & 0x2007, data);
		}
		else if (address == 0x4014)
		{
			// OAM DMA
			// DMA TODO needs 512 cycles
			this.dma.copy(data << 8);
		}
		else if (address == 0x4016)
		{
			// Controller
			this.controller1.write(data);
			this.controller2.write(data);
		}
		else if (address < 0x4018)
		{
			// APU: $4000-$4013, $4015 and $4017
			this.apu.write(address, data);
		}
		else if (address < 0x4020)
		{
			// APU and I/O functionality that is normally disabled
		}
		else
		{
			// ROM
			this.cartridge.mapper.write(address, data);
		}
	}

	public void writeWord(int address, int data)
	{
		this.writeByte(address, data & 0xFF);
		this.writeByte(address + 1, (data >> 8) & 0xFF);
	}

	public int readByte(int address)
	{
		if (address < 0x2000)
		{
			// RAM
			return this.ram.read(address & 0x07FF);
		}
		else if (address < 0x4000)
		{
			// PPU Registers
			return this.ppu.cpuRead(address & 0x2007);
		}
		else if (address == 0x4014)
		{
			// OAM DMA
			return 0;
		}
		else if (address == 0x4016 || address == 0x4017)
		{
			// Controller
			return address == 0x4016 ? this.controller1.read() : this.controller2.read();
		}
		else if (address < 0x4018)
		{
			// APU: $4000-$4013, $4015
			return this.apu.read(address);
		}
		else if (address < 0x4020)
		{
			// APU and I/O functionality that is normally disabled
			return 0;
		}
		else
		{
			// ROM
			return this.cartridge.mapper.read(address);
		}
	}

	public int readWord(int address)
	{
		return (this.readByte(address + 1) << 8 | this.readByte(address)) & 0xFFFF;
	}
};


public class Emulator : IEmulator
{
	ICPU cpu;
	IPPU ppu;
	ICartridge cartridge;
	IRAM ppuRam;
	IRAM cpuRam;
	IBus cpuBus;
	IBus ppuBus;
	IRAM backgroundPalette;
	IRAM spritePalette;
	IDMA dma;
	IAPU apu;

	public Emulator(List<int> nesData, IOptions options)
	{
		standardController1 = new StandardController();
		standardController2 = new StandardController();

		options = this.parseOptions(options);
		sram = Help.NewUint8Array(8192, options.sramLoad);

		var cartridge = new Cartridge(nesData, this.sram);
		var ppuRam = new RAM(1024 * 2, 0x2000); // 0x2000 ~ 0x2800
		var cpuRam = new RAM(1024 * 2, 0); // 0x0000 ~ 0x0800
		var backgroundPalette = new RAM(16, 0x3F00); // 0x3F00 ~ 0x3F10
		var spritePalette = new RAM(16, 0x3F10); // 0x3F10 ~ 0x3F20
		var dma = new DMA();
		var ppuBus = new PPUBus();
		var ppu = new PPU((pixels) => options.onFrame(this.parsePalettePixels(pixels)));
		var cpuBus = new CPUBus();
		var cpu = new CPU();
		var interrupt = new Interrupt();
		var apu = new APU(options.sampleRate, options.onSample);

		cpu.bus = cpuBus;

		ppu.interrupt = interrupt;
		ppu.bus = ppuBus;
		ppu.mapper = cartridge.mapper;

		apu.SetCpuBus(cpuBus);
		apu.SetInterrupt(interrupt);

		dma.cpu = cpu;
		dma.ppu = ppu;

		interrupt.cpu = cpu;

		ppuBus.cartridge = cartridge;
		ppuBus.ram = ppuRam;
		ppuBus.backgroundPallette = backgroundPalette;
		ppuBus.spritePallette = spritePalette;

		cpuBus.cartridge = cartridge;
		cpuBus.ram = cpuRam;
		cpuBus.ppu = ppu;
		cpuBus.dma = dma;
		cpuBus.controller1 = standardController1;
		cpuBus.controller2 = standardController2;
		cpuBus.apu = apu;

		cartridge.mapper.interrupt = interrupt;

		this.cpu = cpu;
		this.ppu = ppu;
		this.ppuRam = ppuRam;
		this.cpuRam = cpuRam;
		this.cpuBus = cpuBus;
		this.ppuBus = ppuBus;
		this.backgroundPalette = backgroundPalette;
		this.spritePalette = spritePalette;
		this.dma = dma;
		this.apu = apu;
		this.cartridge = cartridge;
		this.standardController1 = standardController1;
		this.standardController2 = standardController2;

		cpu.reset();
	}

	public override void clock()
	{
		this.cpu.clock();
		this.apu.clock();
		this.ppu.clock();
		this.ppu.clock();
		this.ppu.clock();
	}

	public override void frame()
	{
		int frame = ((PPU)this.ppu).frame;
		while (true)
		{
			this.clock();

			int newFrame = ((PPU)this.ppu).frame;
			if (newFrame != frame)
			{
				break;
			}
		}
	}

	List<int> parsePalettePixels(List<int> pixels)
	{
		List<int> arr = new List<int>(pixels.Count);
		foreach (var p in pixels)
		{
			arr.Add((int)Global.TABLE[p]);
		}

		return arr;
	}

	IOptions parseOptions(IOptions options = null)
	{
		if (options == null)
			options = new IOptions();

		if (options.sampleRate == 0)
			options.sampleRate = 48000;
		if (options.onSample == null)
			options.onSample = (a) => { };
		if (options.onFrame == null)
			options.onFrame = (a) => { };
		if (options.sramLoad == null)
			options.sramLoad = Help.NewUint8Array(8192);
		return options;
	}
};

class Screen
{
	Image image;

	bool isTrimBorder = false;
	public Image Image
	{
		get { return image; }
	}

	public Screen()
	{
		image = Image.Create(256, 240, false, Image.Format.Rgba8);
	}

	static Color GetColorF(int color)
	{
		return new Color((float)(color >> 16 & 0xFF) / 0xFF,
			(float)(color >> 8 & 0xFF) / 0xFF,
			(float)(color >> 0 & 0xFF) / 0xFF,
			1.0f);
	}

	public void onFrame(List<int> frame)
	{
		for (var y = 0; y < 240; y++)
		{
			if (this.isTrimBorder && (0 <= y && y <= 7 || 232 <= y && y <= 239))
			{
				continue;
			}

			for (var x = 0; x < 256; x++)
			{
				if (this.isTrimBorder && (0 <= x && x <= 7 || 249 <= x && x <= 256))
				{
					image.SetPixel(x, y, Colors.White);
					continue;
				}

				var offset = y * 256 + x;
				//*(byte*)(pBackBuffer + 0) = (byte)((frame[offset] >> 0) & 0xFF);      // B
				//*(byte*)(pBackBuffer + 1) = (byte)((frame[offset] >> 8) & 0xFF);      // G
				//*(byte*)(pBackBuffer + 2) = (byte)((frame[offset] >> 16) & 0xFF);     // R
				//*(byte*)(pBackBuffer + 3) = 255;        // A
				image.SetPixel(x, y, GetColorF(frame[offset]));
			}
		}
	}
}

class Audio
{
	// public IEmulator emulator;

	const int BUFFER_SIZE = 2048;
	//private ctx = new AudioContext({
	//  44100 sampleRate,
	//});
	//private var source =  this.ctx.createBufferSource();
	//private var scriptNode =  this.ctx.createScriptProcessor(BUFFER_SIZE, 0, 1);
	//private var buffer =  [];
	// private Queue<double> queue = new Queue<double>(44100);
	public void start()
	{
		//this.scriptNode.onaudioprocess = e => this.process(e);

		//this.source.connect(this.scriptNode);
		//this.scriptNode.connect(this.ctx.destination);
		//this.source.start();
	}

	public int sampleRate
	{
		get
		{
			return 44100;
			// return this.ctx.sampleRate;
		}
	}

	public void onSample(double volume)
	{
		// this.buffer.push(volume);
		// this.queue.Enqueue(volume);
	}

	private void process(object e)
	{
		//var outputData =  e.outputBuffer.getChannelData(0);

		// if (this.buffer.Count >= outputData.Count) {
		//   for (var sample = 0; sample < outputData.Count; sample++) {
		//     outputData[sample] = this.buffer.shift();
		//   }
		// } else {
		//   // Scale
		//   for (var sample = 0; sample < outputData.Count; sample++) {
		//     outputData[sample] = this.buffer[parseInt((sample * this.buffer.Count / outputData.Count) as any, 10)];
		//   }

		//   this.buffer = [];
		// }
	}
}

public partial class TsNes : Node
{
	Emulator emulator;
	Screen screen;
	Audio audio;

	public void Save(string content)
	{
		using var file = FileAccess.Open("user://save_game.dat", FileAccess.ModeFlags.Write);
		file.StoreString(content);
	}

	public List<int> Load()
	{
		using var file = FileAccess.Open("res://JJZS.nes", FileAccess.ModeFlags.Read);
		var len = file.GetLength();
		var buffer = file.GetBuffer((long)len);

		List<int> list = new List<int>((int)len);
		foreach (var b in buffer)
		{
			list.Add(b);
		}
		return list;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey)
		{
			StandardControllerButton button = StandardControllerButton.A;
			switch (eventKey.Keycode)
			{
				case Key.W:
					button = StandardControllerButton.UP;
					break;
				case Key.S:
					button = StandardControllerButton.DOWN;
					break;
				case Key.A:
					button = StandardControllerButton.LEFT;
					break;
				case Key.D:
					button = StandardControllerButton.RIGHT;
					break;
				case Key.Enter:
					button = StandardControllerButton.START;
					break;
				case Key.Slash: //Key.RightShift:
					button = StandardControllerButton.SELECT;
					break;
				case Key.L:
					button = StandardControllerButton.A;
					break;
				case Key.K:
					button = StandardControllerButton.B;
					break;
				default:
					return;
			}

			emulator.standardController1.updateButton(button, eventKey.Pressed);
			// emulator.standardController2.updateButton(button, eventKey.Pressed);
		}
		// if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
		//     GetTree().Quit();
		// else if(even)

	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.InitAudioStream();
		screen = new Screen();
		// audio = new Audio();

		var options = new IOptions();
		options.sampleRate = 44100;// audio.sampleRate;
		options.onSample = OnSample;
		options.onFrame = screen.onFrame;
		options.sramLoad = null;
		//sramLoad: (() => {
		//  if (localStorage.getItem(filename)) {

		//    return Uint8Array.from(JSON.parse(localStorage.getItem(filename)));
		//  }
		//})(),
		var nesData = Load();
		emulator = new Emulator(nesData, options);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		emulator.frame();
		var texture = ImageTexture.CreateFromImage(screen.Image);
		// sprite2D.Texture = texture;
		GetNode<Sprite2D>("Sprite2D").Texture = texture;
		if (playback != null)
			FillBuffer();
	}

	float sample_hz = 44100.0f;
	float pulse_hz = 22050.0f;
	float phase = 0.0f;

	AudioStreamPlayer audioPlayer;
	AudioStreamGenerator generator;
	AudioStreamGeneratorPlayback playback = null; // Actual playback stream, assigned in _ready().
												  // Called when the node enters the scene tree for the first time.
	void InitAudioStream()
	{
		//$Player.stream.mix_rate = sample_hz # Setting mix rate is only possible before play().
		audioPlayer = GetNode<AudioStreamPlayer>("Player");
		generator = audioPlayer.Stream as AudioStreamGenerator;
		audioQueue = new Queue<float>((int)generator.MixRate);
		// 连接ProcessFrame信号和处理方法
		GetTree().ProcessFrame += OnProcessFrame;
	}

	private void OnProcessFrame()
	{
		if (playback == null)
		{
			audioPlayer.Play();
			// 处理ProcessFrame信号的逻辑代码
			//await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			playback = audioPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
			//_fill_buffer() # Prefill, do before play() to avoid delay.
			FillBuffer();
			GetTree().ProcessFrame -= OnProcessFrame;
			GD.Print("sample rate = ", generator.MixRate);
		}
	}

	Queue<float> audioQueue;
	void OnSample(float sample)
	{
		//GD.Print("queue add"+sample);
		audioQueue.Enqueue(sample);
		// playback.PushFrame(new Vector2(sample, sample));
		// GD.Print("sample " + sample);
	}

	void FillBuffer()
	{
		int count = audioQueue.Count;
		// var bufferCount = playback.GetFramesAvailable();
		// 缓冲区应该总是大于音频队列的
		if (count == 0)
		{
			GD.Print("fill sin" + generator.MixRate);
			// 填充一帧的0作为采样数据
			count = (int)generator.MixRate / 60;	// 估计值
			// Random rand = new Random();
			// for (int i = 0; i < count; ++i)
			// {
			// 	playback.PushFrame(Vector2.Zero * rand.NextSingle());
			// }
			FillBuffer(22050);
			return;
		}
		else
		{
			var avail = playback.GetFramesAvailable();
			if (this.audioQueue.Count > avail)
			{
				for (int i = 0; i < avail; ++i)
					playback.PushFrame(Vector2.One * audioQueue.Dequeue());
			}
			else
			{
				while (audioQueue.Count > 0)
				{
					playback.PushFrame(Vector2.One * audioQueue.Dequeue());
				}
			}
			this.audioQueue.Clear();
		}
	}

	public void FillBuffer(int to_fill)
	{
		var increment = pulse_hz / sample_hz;

		var avail = playback.GetFramesAvailable();
		GD.Print("fill buf " + to_fill + ", " + avail);
		while (to_fill > 0)
		{
			var v = Vector2.One * Mathf.Sin(phase * Mathf.Tau) * 2;
			playback.PushFrame(v);// Audio frames are stereo.
			//GD.Print("Push " + v.ToString());
			phase = (phase + increment) % 1.0f;
			to_fill -= 1;
		}
	}
}

