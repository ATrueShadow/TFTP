namespace TFTPTests
{
    [TestClass]
    public class GetFileTests
    {
        [TestMethod]
        public void GetTest_onlyFilename()
        {
            var client = new TFTPClient(IPAddress.Parse("192.168.1.20"), 1069);
            byte[] ogTxtHash, ogPicHash, txtHash, picHash;

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "testTxt.txt")))
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "testTxt.txt"));

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "testPic.png")))
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "testPic.png"));

            client.GetFile("testTxt.txt");

            client = new TFTPClient(IPAddress.Parse("192.168.1.20"), 1069);
            client.GetFile("testPic.png");

            using (var sha512 = SHA512.Create())
            {
                using (var textfile = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "text.txt"), FileMode.Open))
                    ogTxtHash = sha512.ComputeHash(textfile);

                using (var textfile = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "testTxt.txt"), FileMode.Open))
                    txtHash = sha512.ComputeHash(textfile);

                using (var pic = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "pic.png"), FileMode.Open))
                    ogPicHash = sha512.ComputeHash(pic);

                using (var pic = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "testPic.png"), FileMode.Open))
                    picHash = sha512.ComputeHash(pic);
            }

            Assert.IsTrue(ogTxtHash.SequenceEqual(txtHash), "Text file is corrupted.");
            Assert.IsTrue(ogPicHash.SequenceEqual(picHash), "Picture is corrupted.");
        }
    }
}