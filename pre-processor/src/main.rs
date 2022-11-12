extern crate bmp;
use std::path::PathBuf;

use bmp::BmpError;
use glob::glob;

// We are subtracting 12 for the delimiters.
const CHAR_LIMIT: usize = 64000 - 12;
fn main() {
    let mut char_count = 0;
    let mut char_block = 0;
    let mut block_frames = 0;
    let mut frames = 0;
    println!("# block {}", char_block + 1);
    // Iterate over all frames in frames directory.
    for entry in glob("../frames/*.bmp").expect("Failed to read glob pattern.") {
        match entry {
            Ok(path) => {
                let path_clone = path.clone();
                match read_image(path) {
                    Ok(numbers) => {
                        // Encode the current frame using "bad encode" xD
                        match bad_encode(numbers) {
                            // This frame was encoded.
                            Some(line) => {
                                // If we have exceded the char limit, add a delimiter and start the next block.
                                if char_count + line.len() > CHAR_LIMIT {
                                    char_block += 1;
                                    
                                    eprintln!(
                                        "Block {} has {} frames",
                                        char_block,
                                        block_frames + 1
                                    );

                                    // Reset for the next block.
                                    char_count = 0;
                                    block_frames = 0;

                                    // Write the delimiter
                                    println!("# block {}", char_block + 1);
                                }
                                println!("{}", line);
                                block_frames += 1;
                                frames += 1;
                                char_count += line.len();
                            }
                            // This frame wasn't encoded, print the error to stdout.
                            None => {
                                eprintln!("Frame {} could not be encoded.", path_clone.display());
                            }
                        }
                    }
                    // Failed to open the bmp file.
                    Err(err) => eprintln!(
                        "Frame {} could not be read. Reason: {}",
                        path_clone.display(),
                        err
                    ),
                };
            }
            Err(_) => {}
        }
    }
    eprintln!(
        "Finished. {} blocks with {} frames total.",
        char_block,
        frames + 1
    );
}

fn read_image(path: PathBuf) -> Result<Vec<u32>, BmpError> {
    // Open the image.
    let img = bmp::open(path)?;

    let mut black = true;
    let mut counter = 0;
    let mut numbers: Vec<u32> = Vec::new();

    // Iterate over each pixel in the image.
    for (x, y) in img.coordinates() {
        let is_black = img.get_pixel(x, y).r < 128;

        // When the colour changes, save the counter to our numbers before resetting it.
        if is_black != black {
            black = is_black;
            numbers.push(counter.clone());
            counter = 0;
        }
        counter += 1;
    }
    // The loop has finished, save whatever was in the counter.
    numbers.push(counter);

    Ok(numbers)
}

fn bad_encode(numbers: Vec<u32>) -> Option<String> {
    const OFFSET: u32 = 0x00B0;
    numbers
        .into_iter()
        .map(|x| char::from_u32(x + OFFSET))
        .collect()
}
